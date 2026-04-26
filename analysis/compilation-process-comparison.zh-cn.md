# 两份编译跟踪揭示了 AV 引擎的什么秘密

你有没有好奇过，为什么杀毒软件会明显拖慢某个项目的构建，却几乎不影响另一个？答案通常不是"项目越大 = 构建越慢"。实际上要微妙得多——构建系统、编译器工具链、以及 I/O 流量的形态都很关键，而且它们恰好会暴露不同 AV 产品的不同弱点。

我们捕获了两份 Process Monitor 跟踪——一份来自构建 [ripgrep](https://github.com/BurntSushi/ripgrep)（一个 Rust CLI 工具），另一份来自构建 [Roslyn](https://github.com/dotnet/roslyn)（微软的 C#/VB 编译器平台）——然后把它们当作 AV 工作负载画像来拆解。不是问"哪个构建更慢？"，而是问"当一个安全产品坐在文件系统路径上、实时做信任判断时，每个构建在它眼里到底是什么样？"

分析采用核心构建进程视图：我们以基准测试工具 (`avbench.exe`) 为根重建进程树，然后追踪它的子进程，从而过滤掉 ProcMon 噪声、桌面活动和无关服务。支撑表格在 `analysis/compilation-procmon-analysis.md`，管线文档在 `analysis/workload-profile-pipeline.md`，机器可读数据在 `analysis/procmon-summary.json`。

## 简短版

Roslyn 不只是更大的 ripgrep。从操作系统边界看，它是根本不同的工作负载类别。

**Ripgrep** 紧凑且偏原生工具链——大约 20.2 万个核心构建事件，涉及约 3.4k 条唯一文件路径。活动集中在 `cargo.exe`、`rustc.exe`、`link.exe`、构建脚本、PDB、导入/静态库和新鲜原生输出。可以把它理解为密集的制品生成测试：路径不多，但每条都很关键。

**Roslyn** 是一个大规模托管构建图——约 1260 万个核心事件，涉及约 15.38 万条唯一路径。跟踪扩散到项目文件、`.props`/`.targets`、源文件、引用程序集、NuGet 缓存、SDK 文件、分析器 DLL、生成代码，以及 `VBCSCompiler.exe` 编译器服务器。它是路径扇出和元数据压力测试，外加一波大规模新生成 DLL 带来的次级冲击。

它们都叫"编译"，但拷问的是 AV 产品完全不同的部分。一个关注原生制品创建，另一个关注大规模托管构建图遍历。

## 背景

[Ripgrep](https://github.com/BurntSushi/ripgrep) 是一个面向大型代码库和日常开发的递归正则搜索工具。用 [Cargo](https://doc.rust-lang.org/cargo/commands/cargo-build.html) 构建它，意味着编译本地包、依赖包，以及 crate 定义的所有[构建脚本](https://doc.rust-lang.org/cargo/reference/build-scripts.html)。

[Roslyn](https://github.com/dotnet/roslyn) 是开源的 C#/VB 编译器平台。对它运行 [`dotnet build`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build)，会引入 [MSBuild](https://learn.microsoft.com/en-us/visualstudio/msbuild/incremental-builds?view=visualstudio)、解决方案和项目求值、[NuGet](https://learn.microsoft.com/en-us/nuget/concepts/msbuild-props-and-targets) 包状态、SDK 导入、增量输入/输出检查，以及编译器服务器行为。所以 Roslyn 的跟踪里才会塞满存在性检查、时间戳探测、打开/查询操作、引用读取和生成输出。

## 真正的发现

说"Roslyn 更大"没错，但等于什么都没说。真正有意思的发现是：ripgrep 和 Roslyn 给*不同的 AV 决策路径*施压。

Ripgrep 提出一个聚焦的原生构建问题：当可信的开发工具在短时间内集中产出新的原生制品——PDB、导入库、Rust 元数据、可执行文件——会发生什么？

Roslyn 提出一个铺得很开的托管构建问题：当构建系统要在庞大的项目文件、MSBuild 导入、SDK 文件、引用程序集、NuGet 资产、分析器 DLL、生成代码和新 DLL 输出图谱中反复打开、探测、读取和写入时，又会发生什么？

这是两个截然不同的安全问题。一个产品可以漂亮地搞定其中一个，却在另一个上翻车——而且这不是偶然。一个 Microsoft SDK 允许列表做得很好的产品，可能轻松扫过 Roslyn 的大部分输入侧，但新鲜本地 DLL 开始落地时就卡住了。一个元数据查询过滤很高效的产品，可能扛住 Roslyn 的项目图，但因为把新鲜原生可执行文件当成值得仔细检查的东西而拖慢 ripgrep。

## 为什么偏偏选这一对

"编译"不是单一工作负载——它是一整个家族。一次真实的开发者构建可能涉及依赖发现、源文件读取、元数据检查、编译器进程树、镜像加载、链接器输出、生成 DLL/EXE 写入、包缓存、云端信誉查询，以及围绕开发工具的策略判断。Ripgrep 和 Roslyn 落在这个空间里截然不同的角落。

**Ripgrep 作为原生构建代表：**

- 它是一个真实应用，不是"hello world"式的合成测试。Cargo 编排依赖图并反复调用 `rustc`，工作负载包含真实的包图编排。
- 在 Windows/MSVC 上，最终路径包括原生链接器/资源工具、PDB、导入库、对象文件和可执行文件创建。
- 构建规模足够小，AV 开销不会被庞大构建系统的基线淹没。
- 它提出的 AV 问题很尖锐：产品是否会惩罚新鲜原生输出和链接器/编译器制品？

**Roslyn 作为托管构建代表：**

- 它是一个大型、真实的 .NET 编译器平台仓库，包含大量项目、源文件、资源、生成文件、分析器、测试和引用依赖。
- `dotnet build` 使用 MSBuild，自然会引入项目求值、NuGet/缓存行为、SDK 解析和 target/import 处理。
- MSBuild 的增量逻辑建立在输入/输出关系和时间戳之上——即使某些工作可以跳过，它也得检查大量文件来搞清楚。
- 跟踪捕获了 `VBCSCompiler.exe`，所以它包含编译器服务器的输入扫描，而不只是短生命周期的编译器调用。
- 它提出的 AV 问题更宽泛：产品能否处理巨大的文件图扇出和生成的托管程序集输出，而不让每次打开/查询的开销失控？

两者搭配可以避免陷入单一工作负载的虚假叙事。单用 ripgrep，主要测试原生制品创建和 Rust/MSVC 工具链行为。单用 Roslyn，主要测试 MSBuild/NuGet/引用程序集遍历和托管 DLL 输出。合在一起，才能覆盖两类对用户来说像是同一件事、但对 AV 引擎来说*完全不同*的开发者现实。

## 深入底层：为什么跟踪会分叉

分裂始于构建系统层面。

Cargo 的活儿相对直接：编译包及其依赖。在这份跟踪里，就是 `cargo.exe` 编排、大量 `rustc.exe` 进程、构建脚本，以及 MSVC 链接器/资源工具。构建核心围绕编译 crate 和在 `target\release` 下写入制品。只要 Rustup 和 Cargo 缓存到位，路径空间基本可控。

MSBuild 的职责要宽得多。对一整个解决方案执行 `dotnet build`，需要求值项目文件、导入的 `.props` 和 `.targets`、SDK 文件、包资产、目标框架、分析器配置、引用程序集、生成输出，以及输入/输出新鲜度。在真正开始编译之前，它就一直在问文件系统：这个文件存在吗？哪个 target 生效？哪个引用胜出？这个输出过期了吗？这就是为什么一个 .NET 构建即使最终产出的二进制只占触达路径的一小部分，也会产生大量 `CreateFile`、`QueryOpen`、`QueryDirectory` 和 `QueryNetworkOpenInformationFile` 事件。

Roslyn 之所以会放大这种效应，是因为它*本身就是*一个编译器平台。它包含 C# 和 VB 编译器、工作区、分析器、测试、源生成器、资源、`.editorconfig` 和 `.globalconfig` 输入、引用程序集使用，以及多目标输出。这不只是"编译很多 `.cs` 文件"，而是"求值一个巨大的 .NET 构建图、解析大量引用、运行编译器基础设施、输出很多托管程序集"。

Ripgrep 则放大了另一个方向。Rust 编译会创建 crate 元数据和库（`.rmeta`、`.rlib`），从 Rustup 工具链库读取，在 Windows/MSVC 上写入 PDB 和原生库，并调用链接器/资源工具生成最终输出。与其说是路径扇出，不如说是集中式制品生产。

## 跟踪在五个维度上真正的差异

**1. 路径扇出 vs. 制品集中。** Roslyn 触达约 15.38 万条唯一文件路径。Ripgrep 约 3.4k。这个 45 倍的差距不只是"更大"——它从根本上改变了安全问题。每一条不同路径都是 AV 引擎的又一个决策点：这个哈希已知吗？签名者可信吗？这是包文件还是新生成的输出？热缓存假设随着路径集爆炸会越来越不可靠。

**2. 元数据压力。** Roslyn 有数百万次打开/查询操作，因为 MSBuild 和 .NET SDK 解析本质上就是路径决策系统。构建系统得先拷问文件系统一遍，才知道该编译、复制、跳过还是生成什么。Ripgrep 也有元数据工作，但不像 Roslyn 那样被构建图求值支配。

**3. 输出类型。** Ripgrep 创建的类可执行输出更少，但它们是被链接器/PDB/库副作用包围的原生构建制品。Roslyn 在 `artifacts\obj` 下创建大量新 DLL。AV 引擎对新创建的可执行内容和源文本、已知平台程序集的处理方式往往很不一样，所以输出类别和数量同样重要。

**4. 信任形态。** Roslyn 有大量可能已知的 Microsoft/SDK/引用程序集活动——但*同时*也有大量新鲜本地输出。Ripgrep 的 Microsoft 输入更少，Rustup/Cargo 用户缓存活动占比更高，新鲜原生输出更少但更集中。这就是为什么光看 DLL 数量会误导：Microsoft 引用 DLL、NuGet 包 DLL、本地新生成 DLL 不一定走同一条 AV 逻辑。

**5. 阶段结构。**

| 阶段 | ripgrep | Roslyn |
| --- | --- | --- |
| 编排 | Cargo crate 图 | dotnet/MSBuild 解决方案图 |
| 编译器核心 | 多个 `rustc.exe` crate 编译 | `dotnet.exe` + `VBCSCompiler.exe` 编译器服务器读取 |
| 元数据压力 | 中等 | 很高 |
| 输出压力 | 集中的原生/PDB/lib/rmeta | 大量托管 DLL/XML/cache/resources |
| 信任画像 | Rustup/Cargo 缓存 + 新鲜原生输出 | Microsoft SDK/NuGet/引用读取 + 新鲜 DLL |
| 网络噪声 | 接近本地 | 观察到 restore/cache/类似遥测的 HTTPS |

## AV 产品是怎么踩坑的

现代 AV 产品早就不只是挂在文件读取上的字节扫描器了。一个典型引擎会混合 minifilter 回调、路径策略、哈希缓存、签名者信誉、云端流行度、脚本规则、行为模型、可执行内容启发式、进程信誉，有时还有专门的开发工具处理。Ripgrep 和 Roslyn 压到的是这些子系统的不同组合。

分叉发生在这些地方：

**每次打开和元数据查询的开销。** Roslyn 是更敏锐的探针。如果一个产品在 `CreateFile`、`QueryOpen`、目录查询或元数据检查上增加哪怕一点延迟，Roslyn 就能把这个惩罚放大数百万倍。同一个产品在 ripgrep 上可能看着没事，因为 ripgrep 不会以同样方式做文件系统扇出。

**新鲜 EXE/DLL 输出扫描。** 两个工作负载都会触发，但口味不同。Ripgrep 产出原生可执行文件和链接器制品。Roslyn 产出大量托管 DLL。会深度检查新建可执行文件、等待云端信誉、或标记"编译器刚生成了一个二进制"的产品，都可能拖慢任一工作负载——但 Roslyn 的总输出面更大，ripgrep 的原生链接器面更密集。

**签名者和路径信誉。** Roslyn 读取远更多 Microsoft/SDK/引用内容。一个对 Microsoft 发布者/路径信誉处理很强的产品，可以让 Roslyn 输入侧的一大块走快速路径——但新鲜输出是本地新生成的，这条快捷路径覆盖不到。Ripgrep 读取的 Microsoft 路径制品更少，Rustup/Cargo 用户缓存文件更多，所以更依赖哈希/云端流行度而非 Authenticode 捷径。

**包缓存信任。** NuGet 和 Cargo/Rustup 缓存属于不同的信誉生态。NuGet 包可能常见也可能有包签名，但里面的 DLL 不一定有 Authenticode 签名。Rustup/Cargo 制品对开发者来说可能稳定常见，但很少落在 Microsoft 允许列表上。不同 AV 厂商在这里给出的判断可能差异很大。

**编译器服务器 vs. 短生命周期进程。** Roslyn 的 `VBCSCompiler.exe` 把大量编译器输入读取集中在一个长生命周期进程里。围绕进程身份和生命周期建立信誉的产品，看到的信号与 ripgrep 的大量短生命周期 `rustc.exe` 调用截然不同。

**FAST IO 回退。** Roslyn 有远更多 `FAST IO DISALLOWED` 事件，这可能意味着过滤器正在把文件系统推到更慢的路径。如果一个产品经常在元数据密集工作负载上阻止 fast I/O，Roslyn 会把这种行为暴露得更加清晰。

**云端缓存 vs. 本地缓存。** Roslyn 庞大的唯一路径集合会触发远更多缓存和信誉决策。Ripgrep 则可能由更少但更"可疑"的新鲜制品主导。一个本地哈希缓存强、但云端信誉较慢的产品，可能表现出巨大的首次运行惩罚，后续运行则大幅改善。

**行为层的"开发工具"规则。** 编译器天生就会写入可执行代码——这正是它们让 AV 引擎头疼的原因。一些产品会特殊处理可信工具链；另一些则把代码生成、链接器输出或未签名二进制当作值得关注的行为。Ripgrep 测试原生编译器/链接器行为。Roslyn 测试大规模托管程序集生成。

这就是为什么产品排名会在两张图之间翻转。一个在 .NET SDK 允许列表和低成本元数据 hook 上做得很好的产品，在 Roslyn 上可能看起来不错，直到生成 DLL 阶段开始反噬。一个文件打开效率高、但对原生可执行创建很激进的产品，扛得住 Roslyn 的元数据风暴，却可能在 ripgrep 的原生输出上失手。

## 数据细看

### 规模一览

| 指标 | ripgrep | Roslyn | Roslyn / ripgrep |
| --- | ---: | ---: | ---: |
| ProcMon CSV 大小 | 87 MB | 3.7 GB | 43x |
| 捕获事件总数 | 463,658 | 15,194,989 | 33x |
| 构建相关事件，排除 ProcMon/System | 267,501 | 13,428,524 | 50x |
| 核心构建进程事件 | 201,969 | 12,624,681 | 63x |
| 核心进程树规模 | 79 | 124 | 1.6x |
| 唯一核心文件路径 | 3,395 | 153,774 | 45x |
| 唯一核心注册表路径 | 6,050 | 5,477 | 0.9x |
| 跟踪时间窗口 | 57s | 11m 57s | 12.6x |

最扎眼的一行是唯一核心文件路径。Roslyn 触达的唯一路径约为 ripgrep 的 45 倍。这就是跟踪开始像 AV 压力测试、而不只是"大型构建"的地方——路径越多，缓存未命中、元数据决策、信誉查询、哈希机会和过滤回调就越多。

### 操作族分布

| 操作族 | ripgrep 数量 | ripgrep % | Roslyn 数量 | Roslyn % |
| --- | ---: | ---: | ---: | ---: |
| file | 136,997 | 67.8% | 9,829,880 | 77.9% |
| registry | 51,132 | 25.3% | 1,342,861 | 10.6% |
| other | 10,430 | 5.2% | 1,388,455 | 11.0% |
| process/thread | 3,095 | 1.5% | 14,603 | 0.1% |
| profiling | 270 | 0.1% | 34,252 | 0.3% |
| network | 45 | 0.0% | 14,630 | 0.1% |

两个构建都由文件活动压倒性主导。Roslyn 更甚，因为文件图*本身就是*它的工作负载。Ripgrep 的注册表占比依然可见，因为原生 MSVC 路径会探测系统、SDK、Visual Studio 和运行时配置。Roslyn 绝对量上的注册表工作也不少，但文件图大得注册表只能算舍入误差。

### 谁在干什么

| 进程 | ripgrep 事件 | 角色 |
| --- | ---: | --- |
| `rustc.exe` | 93,193 | Rust 编译器工作；读取 Rust 源码/元数据/库，写入 crate 元数据/制品。 |
| `link.exe` | 32,788 | MSVC 链接器；写入 PDB/最终原生制品，读取 Windows/MSVC 导入库。 |
| `VCTIP.EXE` | 25,858 | Visual Studio/MSVC 辅助活动，主要是注册表/工具链探测。 |
| `avbench.exe` | 23,617 | 基准测试 harness 和进程编排。 |
| `cargo.exe` | 21,983 | Cargo 包/构建图编排。 |

| 进程 | Roslyn 事件 | 角色 |
| --- | ---: | --- |
| `dotnet.exe` | 10,053,215 | .NET CLI/MSBuild 宿主；主导项目图、restore/构建元数据、SDK、NuGet 和输出活动。 |
| `VBCSCompiler.exe` | 2,436,411 | Roslyn 编译器服务器进程；大量读取源文件、引用、分析器和编译器输入。 |
| `avbench.exe` | 51,301 | 基准测试 harness 和进程编排。 |
| `Conhost.exe` | 40,145 | 命令行构建的控制台宿主活动。 |
| `VsdConfigTool.exe` | 37,482 | Visual Studio/dotnet 配置辅助活动，主要是注册表。 |

进程分布是最干净的指纹之一。Ripgrep 是一个小生态——Cargo、rustc、链接器，加上一些工具链辅助程序。Roslyn 则压倒性地由 `dotnet.exe` 加编译器服务器构成，其他一切都是背景噪声。

### 文件操作画像

| 操作 | ripgrep | Roslyn | 解读 |
| --- | ---: | ---: | --- |
| `CreateFile` | 18,983 | 2,206,227 | Roslyn 打开了远多于 ripgrep 的文件和目录。 |
| `QueryOpen` | 3,257 | 1,615,022 | Roslyn 执行大量存在性/元数据检查。 |
| `QueryNetworkOpenInformationFile` | 647 | 1,050,266 | Roslyn 按路径大量查询文件元数据。 |
| `ReadFile` | 13,961 | 917,206 | Roslyn 读取更多源文件、程序集、配置和生成输入。 |
| `WriteFile` | 33,434 | 290,668 | Roslyn 绝对写入更多，但 ripgrep 按比例明显更偏写入。 |
| `QueryDirectory` | 1,041 | 195,120 | Roslyn 执行更广泛的目录/项目/包枚举。 |
| `Load Image` | 2,225 | 6,823 | 两者都加载 EXE/DLL；已计为文件活动。 |
| `RegOpenKey` | 19,384 | 361,562 | 两者都查询环境/工具链/运行时配置。 |
| `RegQueryValue` | 15,057 | 536,724 | Roslyn 在绝对值上有更多注册表值查询。 |

Roslyn 的 `CreateFile` 约为 ripgrep 的 116 倍，`QueryOpen` 约 496 倍，`QueryNetworkOpenInformationFile` 约 1623 倍。这就是 MSBuild 构建图求值的典型签名：项目文件、导入的 `.props`/`.targets`、目标框架检查、引用程序集、包文件、生成输出和 up-to-date 决策全部堆叠。

而 ripgrep 的 `WriteFile` 事件只比 Roslyn 少约 8.7 倍，尽管核心事件总数少了 63 倍。这就是关键信号：ripgrep *按比例*明显更偏写入。完美契合 Rust/原生构建的形态——编译器/链接器输出、PDB、`.rlib`、`.rmeta`、`.lib`、`.o`、临时导入库和最终二进制。

## 解剖 Ripgrep 跟踪

Ripgrep 的核心活动集中在：

| 路径根 | 核心事件 | 含义 |
| --- | ---: | --- |
| `C:\bench\ripgrep` | 58,442 | 仓库源码和 `target\release` 输出 |
| `C:\Users\User` | 55,368 | Rustup 工具链、Cargo 缓存/配置、临时链接器/编译器文件 |
| `HKLM\System\CurrentControlSet` | 28,032 | 系统/设备/工具链注册表查询 |
| `C:\Windows\System32` | 18,402 | 系统 DLL 和 Windows 运行时输入 |
| `C:\Program Files (x86)\Microsoft Visual Studio` | 4,316 | MSVC 链接器/工具链文件 |
| `C:\Program Files (x86)\Windows Kits` | 2,686 | Windows SDK 导入库/资源 |

主要文件扩展名：

| 扩展名 | 事件 | 解读 |
| --- | ---: | --- |
| `.dll` | 25,201 | Rust 编译器驱动 DLL、系统 DLL、运行时/工具链 DLL |
| `.rlib` | 18,623 | Rust 库制品 |
| `.pdb` | 16,946 | 调试符号，尤其是链接器/编译器输出 |
| `.lib` | 14,438 | 原生/MSVC/Windows 导入库 |
| `.rmeta` | 13,048 | Rust crate 元数据 |
| `.rs` | 10,235 | Rust 源文件 |
| `.o` | 7,259 | 对象文件 |
| `.exe` | 4,704 | 编译器/链接器/工具/构建可执行文件 |

主要写入扩展名：

| 扩展名 | 写入事件 | 核心写入占比 |
| --- | ---: | ---: |
| `.pdb` | 15,575 | 45.9% |
| `.lib` | 11,388 | 33.6% |
| `.rmeta` | 4,805 | 14.2% |
| `.o` | 748 | 2.2% |
| `.a` | 747 | 2.2% |

简单概括 ripgrep：它不会在几十万条路径上爆炸式扩散，而是把活动集中在 `target\release`、Rustup/Cargo 缓存、PDB、导入/静态库和原生链接器输出。任何在制品创建、PDB 写入、链接器临时文件或新建可执行/库内容上变贵的产品，都会在这里显形。

网络活动几乎可忽略——仅 45 个核心网络事件——让 ripgrep 成为一个干净的、基本本地的编译工作负载。

## 解剖 Roslyn 跟踪

Roslyn 的核心活动集中在：

| 路径根 | 核心事件 | 含义 |
| --- | ---: | --- |
| `C:\bench\roslyn` | 5,786,039 | 源码树、生成文件、`artifacts\obj`、项目图 |
| `C:\Users\User` | 3,793,353 | NuGet 缓存、dotnet 遥测/缓存、用户包内容 |
| `HKLM\System\CurrentControlSet` | 1,103,457 | 系统/设备/运行时注册表查询 |
| `C:\Program Files\dotnet` | 924,617 | .NET SDK、MSBuild、分析器、targets、运行时文件 |
| `C:\Program Files (x86)\Reference Assemblies` | 528,178 | .NET Framework/引用程序集 |

主要文件扩展名：

| 扩展名 | 事件 | 解读 |
| --- | ---: | --- |
| `.dll` | 4,859,576 | 引用程序集、编译器/分析器/运行时 DLL、构建输出 |
| `.cs` | 747,779 | C# 源文件 |
| `.sha512` | 474,300 | NuGet 包验证/缓存元数据 |
| `.targets` | 241,580 | MSBuild target 导入 |
| `.xml` | 204,553 | 文档/配置/生成 XML |
| `.props` | 176,379 | MSBuild 属性导入 |
| `.resx` | 169,506 | 资源 |
| `.csproj` | 168,300 | 项目文件 |
| `.editorconfig` | 168,204 | 分析器/编译器配置 |
| `.globalconfig` | 161,833 | 分析器/编译器全局配置 |
| `.vb` | 161,727 | Roslyn 中的 Visual Basic 源文件 |

主要写入扩展名：

| 扩展名 | 写入事件 | 核心写入占比 |
| --- | ---: | ---: |
| `.dll` | 214,373 | 49.5% |
| `.tmp` | 76,748 | 17.7% |
| `.cache` | 39,936 | 9.2% |
| `.xml` | 26,756 | 6.2% |
| `.resources` | 21,138 | 4.9% |

Roslyn 的跟踪像是 .NET 生态的导览：

- **MSBuild 项目求值和 target 执行：** `.csproj`、`.props`、`.targets`、`.editorconfig`、`.globalconfig`。
- **依赖和包基础设施：** `.nuget`、`.sha512`、包缓存文件、生成的 NuGet props/targets。
- **编译器输入：** `.cs`、`.vb`、`.resx`、引用程序集、分析器 DLL。
- **编译器/构建输出：** `artifacts\obj`、生成的 `.cs`、输出 `.dll`、`.xml`、`.resources`、`.cache`、临时文件。
- **编译器服务器行为：** 巨大的 `VBCSCompiler.exe` 读取足迹，把编译器输入扫描集中在一个长生命周期进程中。

这种工作负载会惩罚薄弱的文件元数据缓存或昂贵的打开/查询 hook。即使读取的字节量并不总是很大，唯一路径和探测的数量也已经极为庞大。

网络活动按比例仍然很小，但并非零：14,630 个核心网络事件，主要目标包括发往 Microsoft/Akamai 相关端点的 HTTPS 流量。结合命令和 NuGet/缓存路径，可能的原因是 restore、SDK/工作负载元数据、包/缓存验证，或接近遥测的流量。如果目标是严格本地的 AV 测试，未来的 Roslyn 运行应使用预先 restore 的包、`--no-restore`、禁用遥测，并最好加入断网对照。

## 注册表和配置行为

Ripgrep 的注册表活动按比例更高——占核心事件的 25.2%。热点根包括 `HKLM\System\CurrentControlSet`、`HKLM\SOFTWARE\Microsoft` 和 Visual Studio/MSVC 相关位置。经典的 Windows 原生构建操作：编译器发现、链接器设置、Windows SDK 查找、运行时配置、Visual Studio 工具链管线。

Roslyn 的注册表活动绝对数量更大，但占比更小：134 万事件，占 10.6%。主要集中在 `HKLM\System\CurrentControlSet` 以及 .NET/Visual Studio/SDK 查找。较小的百分比不意味着注册表无关紧要——只是文件图太大，大到注册表退到了背景辐射的位置。

对 AV 性能来说，注册表操作通常不如文件打开和写入重要。但注册表密集的设置阶段仍可能与自我保护 hook、行为监控、策略检查和进程/工具链信誉相交。

## 错误和非成功结果

两份跟踪都有大量非成功结果——这对构建系统来说完全正常：

| 结果 | ripgrep | Roslyn | 解读 |
| --- | ---: | ---: | --- |
| `SUCCESS` | 149,553 | 9,374,926 | 已完成操作 |
| `NAME NOT FOUND` | 18,759 | 1,034,436 | 探测可选路径/文件/注册表键 |
| `FAST IO DISALLOWED` | 9,003 | 1,649,094 | Windows 回退到更慢的 IRP 路径；过滤器存在时常见 |
| `REPARSE` | 4,230 | 128,917 | 路径 reparse/符号链接/junction 行为 |
| `NO MORE FILES` | 347 | 70,608 | 目录枚举结束 |

`NAME NOT FOUND` 本身不是失败信号。构建系统会不断探测可选文件。Roslyn 让这一点特别明显，因为 MSBuild 和 NuGet 会评估大量可能的 import、目标框架、包资产、SDK 文件、生成输出和引用位置。

`FAST IO DISALLOWED` 值得关注，因为文件系统过滤器会影响 fast I/O 路径是否可用。Roslyn 的这类事件多得多，所以一个经常强制走慢路径的产品，会不成比例地伤到 Roslyn。

## 这些差异如何冲击 AV 性能

### 信任谜题

ProcMon 不记录 Authenticode 签名者、catalog 签名、云端流行度或 AV 产品内部的允许列表决策。所以我们无法*证明*某个文件被信任了——管线使用基于路径的信任/信誉桶作为代理指标。

信任的故事并不是单向的：

| 问题 | 跟踪给出的可能答案 |
| --- | --- |
| 哪个工作负载在绝对值上触达更多可能属于 Microsoft/SDK/引用体系的二进制？ | Roslyn。它在 `C:\Program Files\dotnet`、引用程序集、SDK 位置和 Windows DLL 路径下有更多活动。 |
| 哪个工作负载加载更多平台可信 DLL？ | 绝对数量上是 Roslyn；两者的镜像加载主要都是 Windows/SDK 路径。 |
| 哪个工作负载创建更多新鲜的类可执行输出？ | Roslyn 多得多。它生成的 `artifacts\obj` DLL 写入主导类可执行写入。 |
| 哪个工作负载按比例更偏非 Microsoft/用户缓存/工具链形态？ | Ripgrep。它规模更小，但大量活动来自 Rustup/Cargo 用户缓存和新鲜 `target\release` 输出。 |

增强画像的路径桶摘要：

| 指标 | ripgrep | Roslyn |
| --- | ---: | ---: |
| Microsoft OS 路径文件事件 | 22,231 | 44,548 |
| Microsoft SDK / Program Files 路径文件事件 | 8,475 | 1,335,173 |
| 用户包/工具链缓存文件事件 | 36,145 Rustup/Cargo | 2,987,591 NuGet |
| 新鲜构建输出文件事件 | 48,320 | 2,966,226 |
| Microsoft OS/SDK 桶中的类可执行事件 | 21,808 | 1,091,019 |
| 包/工具链缓存桶中的类可执行事件 | 9,795 Rustup/Cargo | 2,587,458 NuGet |
| 新鲜构建输出中的类可执行事件 | 1,404 | 2,195,529 |
| 新鲜构建输出中的类可执行写入 | 58 | 216,285 |

实际影响是：Roslyn 巨大的 DLL/引用足迹并非成本均匀——很多输入可能稳定、已签名、常见，或来自包缓存，产品可能很快信任它们。但 Roslyn 也会创建*大量*全新 DLL，这些本地输出拿不到同样的签名/云端/缓存快捷路径。Ripgrep 文件更少、生成的类可执行写入也更少，但 Rustup/Cargo 缓存和新鲜原生制品可能不在 Microsoft Authenticode 允许列表的覆盖范围内。

### 超越原始数量：速率和加权压力

单看事件数量，回答的是"AV 有多少次介入机会？"，但回答不了"这些机会可能有多贵？"。百分比能看出形态，却隐藏了规模。所以管线报告四个层级：

| 层级 | 回答什么问题 | 为什么重要 |
| --- | --- | --- |
| 绝对数量 | AV 暴露面有多大 | Roslyn 有远更多文件打开、元数据查询、失败探测和写入 |
| 百分比 | 这是什么类型的工作负载 | ripgrep 按比例偏写入；Roslyn 偏元数据/打开 |
| 每秒速率 | 事件流有多密集 | Roslyn 虽然运行更久，但每秒事件更多 |
| 加权压力 | 哪些事件可能更贵 | 新鲜可执行写入和新鲜构建输出比已知 Microsoft OS/SDK 读取权重更高 |

加权压力摘要：

| 指标 | ripgrep | Roslyn |
| --- | ---: | ---: |
| 加权压力分 | 877,977 | 52,713,707 |
| 每秒压力 | 15,369 | 73,520 |
| 每 1,000 事件压力 | 4,347 | 4,175 |
| 最高压力操作组 | write | write |
| 最高压力信任桶 | fresh build output | fresh build output |
| 最高压力阶段 | link/resources | output/write phase |

这个分数是启发式的，不是实测延迟——使用透明权重：元数据查询较低，读取较低/中等，写入更高，镜像加载更高，类可执行写入很高。可能属于 Microsoft OS/SDK 的路径使用较低乘数；新鲜构建输出和用户/包/工具链路径使用更高乘数。

这改变了结论。Roslyn 的总 AV 压力压倒性更高，因为它规模巨大且创建了大量新鲜 DLL 输出。但 ripgrep 每 1,000 事件的压力密度略*高*，因为它较小的事件流集中在写入和新鲜原生制品上。**Roslyn 是更大的总压力测试；ripgrep 是更密集的原生制品压力测试。**

未来运行中值得跟踪的信誉因素：

- Authenticode 签名者和 catalog 签名：Microsoft 签名的 OS/SDK/引用二进制可能比未签名本地输出走更快的 AV 路径。
- 云端流行度和文件年龄：常见 SDK/NuGet/Rustup 文件可能已知；新构建输出则不然。
- 路径和发布者信任：`C:\Windows`、`Program Files\dotnet`、Visual Studio 和 Windows Kits 通常不同于 `C:\bench` 或用户配置文件缓存。
- 生成的类可执行输出：新的 `.exe`/`.dll` 文件可能触发更深的静态、行为或信誉检查。
- 包缓存信任：NuGet 包可能有包签名或很常见，但里面的 DLL 不总是 Authenticode 签名。
- 工具链缓存信任：Rustup/Cargo 制品可能稳定常见，但不是 Microsoft 签名。
- 镜像加载 vs. 文件读取 vs. 文件写入：AV 产品可能区别对待"加载 DLL"、"把 DLL 当数据读取"和"写入新 DLL"。
- 脚本和 MOTW 语义：`.ps1`、`.js`、下载文件和 Zone.Identifier/MOTW 可能显著改变信誉路径。
- 缓存范围：产品本地缓存、云端缓存、文件哈希缓存和 VM 重置行为可能让 first-cloud-seen 与平均运行结果分叉。

### Ripgrep 最容易踩到什么

主要 AV 压力点：

- 扫描新写入的编译器/链接器制品。
- 扫描 PDB、`.lib`、`.rmeta`、`.rlib`、`.o`、`.a` 和最终可执行输出。
- 拦截 `%TEMP%` 下的链接器临时文件。
- 反复读取 Rust 工具链库和 Rust 标准库制品。
- 原生 Windows SDK/MSVC 工具链发现和注册表探测。

可以预期：

- 扫描每一个新建对象/库/调试制品的产品，影响会很高。
- 对 Rust 工具链库有良好路径/哈希缓存的产品，缓存预热后应明显改善。
- 把编译器/链接器输出视为可疑可执行生成行为的产品，即使唯一路径相对较少，也可能增加延迟。

### Roslyn 最容易踩到什么

主要 AV 压力点：

- 横跨源文件、SDK、NuGet、引用程序集、生成文件和输出文件的大量文件打开/查询/读取。
- 大量 `.dll` 读写——分析器、引用程序集、编译器/运行时组件和输出。
- 通过 `.props` 和 `.targets` 执行 MSBuild/NuGet target 与属性求值。
- 通过路径存在性、时间戳、元数据和目录枚举执行增量构建和依赖图检查。
- 编译器服务器进程读取大量编译器输入。

可以预期：

- 每次打开或每次查询过滤较昂贵的产品，即使字节扫描不多也可能受影响。
- VM 重置后缓存复用较弱的产品，会反复为 SDK、NuGet、引用程序集和分析器路径付出代价。
- 对 DLL、源生成器、分析器或新生成程序集应用更重规则的产品，影响会很明显。
- 对大量不同 DLL/源文件/生成路径执行云端信誉检查的产品，first-cloud-seen 影响可能很高。

## 速查表

| 维度 | ripgrep | Roslyn |
| --- | --- | --- |
| 生态 | Rust/Cargo/原生 MSVC 链接 | .NET/MSBuild/Roslyn/NuGet |
| 规模 | 紧凑 | 很大 |
| 唯一文件 | 数千级 | 十万级 |
| 主要压力 | 制品写入和链接器/编译器输出 | 文件图遍历、元数据检查、引用、包 |
| 主要进程形态 | `cargo` + `rustc` + `link` | `dotnet` + `VBCSCompiler` |
| AV 缓存问题 | 产品能否高效处理生成的原生制品？ | 产品能否高效处理庞大的托管图遍历和生成 DLL？ |
| 网络敏感性 | 接近本地 | 观察到一些 restore/元数据/类似遥测的 HTTPS 活动 |

价值在于对比。把它们当作"编译时间"的重复样本，会丢掉真正的信号。

## 下次我们会怎么做

1. **保持图表独立。** Ripgrep 和 Roslyn 值得各有自己的结果——它们的 OS 操作结构差异足够大，单一合并数字会掩盖有趣的产品行为。

2. **作为子工作负载评分，而非重复样本。** 一个产品可以在 ripgrep 上表现优秀、在 Roslyn 上表现糟糕，或反过来，背后有完全站得住脚的技术理由。

3. **增加严格离线的 Roslyn 变体。** 当前跟踪包含一些网络活动，`dotnet build` 可能隐式 restore 或触碰工作负载/包元数据。如果目标是纯本地 AV 开销，应使用预先 restore 的包、`--no-restore` 和禁用遥测。

4. **分别报告首次运行和缓存预热后的表现。** First-cloud-seen 运行捕获产品冷缓存和云端信誉行为；平均运行捕获缓存稳定后的日常开发体验。两者讲述的是不同的故事。

5. **当 Roslyn 是离群值时，检查文件打开/查询延迟。** Roslyn 受路径和元数据行为主导——不是简单的"写入字节数"模型。

6. **当 ripgrep 是离群值时，检查制品创建。** 关注新建的编译器/链接器制品、PDB 写入和临时导入库行为。

## 局限性

ProcMon 跟踪是观察性的——它显示发生了哪些操作，但不能精确说明每个 AV 产品给每个操作增加了多少延迟。数量仍然重要，因为它展示了暴露给文件系统过滤器的攻击面；但要做真正的时延归因，需要每个产品的时序数据或 ETW/WPA 级别的延迟分析。

核心构建进程过滤基于 ProcMon 的进程创建关系。这比静态按进程名过滤更强，但并不完美：它会包括 `Conhost.exe` 这类控制台/辅助子进程，也可能漏掉不是基准进程后代、但执行了构建相关工作的长期系统服务。

生成 JSON 中的字节计数应视为方向性指标。ProcMon 的 `Detail` 字段因操作而异；本分析关注的是操作结构、路径扇出、进程组合、文件类型组合和信誉攻击面——不是精确的字节核算。

## 最后一句话

Ripgrep 是紧凑的原生制品压力测试。Roslyn 是铺得很开的托管图压力测试。它们都是编译工作负载，但提出的是非常不同的 AV 问题。这正是两者都应该留在测试套件里的原因。
