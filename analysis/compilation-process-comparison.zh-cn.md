# 编译跟踪分析：ripgrep 与 Roslyn

这份报告把两份 Process Monitor 跟踪 `tmp/ripgrep.CSV` 和 `tmp/roslyn.CSV` 当作面向 AV 的工作负载画像来读。重点不只是“哪个构建更慢”或“哪份跟踪更大”。对 AV 性能来说，更关键的问题是：当一个安全产品位于文件系统路径上、实时做信誉和信任判断时，它看到的构建行为到底是什么样。

本文的主要结论基于核心构建进程视图，而不是整个桌面环境的完整跟踪。分析器会以对应工作负载的 `avbench.exe` 进程为根，重建进程树并追踪它的子进程。相比静态按进程名过滤，这能更好地排除 ProcMon 自身、大部分桌面噪声，以及无关服务活动。支撑表格在 `analysis/compilation-procmon-analysis.md`，可复现画像层在 `analysis/workload-profile-pipeline.md`，机器可读摘要在 `analysis/procmon-summary.json`。

## 执行摘要

Roslyn 并不是“更大的 ripgrep 构建”。从操作系统边界看，它属于另一类工作负载。

ripgrep 跟踪更紧凑，并且明显偏向原生工具链。它有大约 20.2 万个核心构建事件，涉及约 3.4k 条唯一文件路径。最有意思的活动集中在 `cargo.exe`、`rustc.exe`、`link.exe`、构建脚本、Rust 元数据、PDB、静态/导入库，以及最终原生输出上。从 AV 视角看，ripgrep 是一个密集的制品生成测试：路径较少，但新生成的编译器/链接器输出高度集中。

Roslyn 跟踪则是一个大规模托管构建图。它有约 1260 万个核心构建事件，涉及约 15.38 万条唯一文件路径。跟踪会扩散到项目文件、`.props`/`.targets`、源文件、引用程序集、NuGet 缓存内容、SDK 文件、分析器/编译器 DLL、生成文件，以及 `VBCSCompiler.exe`。从 AV 视角看，Roslyn 是路径扇出和元数据压力测试，同时还有大量新生成 DLL 带来的次级压力面。

这正是这组测试有价值的原因。它们都叫“编译”，但会拷问 AV 产品的不同部分：一个关注原生制品创建，另一个关注大规模托管构建图遍历和生成程序集输出。

## 背景来源

ripgrep 是一个 Rust 编写的命令行搜索工具。它的 README 将其描述为面向大型代码树和日常开发流程的递归正则搜索器。用 Cargo 构建它，意味着要编译本地包、依赖包，以及可能存在的 crate 构建脚本。

Roslyn 是开源的 C# 和 Visual Basic 编译器平台。通过 `dotnet build` 构建它，会引入 MSBuild、解决方案/项目求值、SDK 导入、NuGet/包状态、增量输入输出检查，以及编译器服务器行为。这也是为什么 Roslyn 跟踪中充满存在性检查、时间戳探测、打开/查询操作、引用读取和生成输出。

来源：

- [ripgrep README](https://github.com/BurntSushi/ripgrep)
- [Cargo build command](https://doc.rust-lang.org/cargo/commands/cargo-build.html)
- [Cargo build scripts](https://doc.rust-lang.org/cargo/reference/build-scripts.html)
- [Roslyn repository README](https://github.com/dotnet/roslyn)
- [dotnet build command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build)
- [dotnet restore command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore)
- [MSBuild incremental builds](https://learn.microsoft.com/en-us/visualstudio/msbuild/incremental-builds?view=visualstudio)
- [NuGet MSBuild props and targets](https://learn.microsoft.com/en-us/nuget/concepts/msbuild-props-and-targets)

## 核心论点

核心结论不是简单的“Roslyn 更大”。这是真的，但还不够深入。更有价值的结论是：ripgrep 和 Roslyn 会给不同的 AV 决策路径施压。

ripgrep 提出的是一个紧凑的原生构建问题：当可信的开发工具在短时间内集中创建新的原生制品、PDB、导入/静态库、Rust 元数据文件和最终可执行输出时，会发生什么？

Roslyn 提出的是一个大型托管构建问题：当构建系统要在庞大的项目文件、导入的 MSBuild 文件、SDK 文件、引用程序集、NuGet 资产、分析器/编译器 DLL、生成文件和新 DLL 输出图中反复打开、探测、读取和写入时，会发生什么？

这是两类不同的问题。一个 AV 产品可能很好地处理其中一种，却在另一种上表现糟糕，这并不随机。一个对 Microsoft SDK 有强允许列表策略的产品，可能让 Roslyn 的部分输入侧看起来很便宜，但仍然卡在本地新生成 DLL 的输出阶段。一个元数据查询过滤很高效的产品，可能扛住 Roslyn 的项目图，却因为把新生成的原生可执行文件视为更可疑而拖慢 ripgrep。

## 为什么选择这两个工作负载

“编译”不是单一工作负载。真实的开发者构建可能涉及依赖发现、源文件读取、元数据检查、编译器进程树、镜像加载、链接器输出、生成 DLL/EXE 写入、包缓存、云端信誉，以及围绕开发工具的策略判断。ripgrep 和 Roslyn 落在这个空间里的不同位置。

ripgrep 是紧凑原生构建的代表：

- 它是真实的 Rust 应用，不是合成的“hello world 编译”案例。
- Cargo 会构建依赖图并反复调用 `rustc`，所以工作负载包含包图编排和编译器活动。
- 在 Windows/MSVC 上，最终路径包括原生链接器/资源工具、PDB、导入库、类对象文件制品，以及可执行文件创建。
- 构建规模足够小，AV 开销不会被庞大构建系统本身的基线淹没。
- 它提出的 AV 问题很集中：产品是否会惩罚新生成的原生输出和链接器/编译器制品？

Roslyn 是大型托管构建的代表：

- 它是真实且大型的 .NET 编译器平台仓库，包含许多项目、源文件、资源、生成文件、分析器、测试和引用依赖。
- `dotnet build` 使用 MSBuild，并且在未禁用时可能执行隐式 restore。这自然会引入项目求值、NuGet/缓存行为、SDK 解析和 target/import 处理。
- MSBuild 的增量行为建立在输入/输出关系和时间戳之上；即使某些工作可以跳过，它也必须检查大量文件来判断当前状态是否最新。
- 跟踪中观察到了 `VBCSCompiler.exe`，所以它包含编译器服务器的输入扫描，而不只是短生命周期的编译器进程调用。
- 它提出的 AV 问题更宽：产品能否处理巨大的文件图扇出和生成的托管程序集输出，而不让每次打开/查询的开销爆炸？

这组搭配可以避免基准测试陷入单一工作负载叙事。如果只用 ripgrep，我们主要测试原生制品创建和 Rust/MSVC 工具链行为。如果只用 Roslyn，我们主要测试 MSBuild/NuGet/引用程序集遍历和托管 DLL 输出。两者合在一起，覆盖了两类对用户来说都像“编译”、但对 AV 引擎来说完全不同的开发者现实。

## 它们为什么在机制上不同

差异首先来自构建系统。

Cargo 的职责相对直接：编译包及其依赖。在这份跟踪中，它表现为 `cargo.exe` 编排、大量 `rustc.exe` 进程、crate 构建脚本，以及 MSVC 链接器/资源工具。Cargo 和 rustc 仍然会读取源文件和元数据，但构建核心仍围绕编译 crate、并在 `target\release` 下写入制品。只要 Rustup 和 Cargo 缓存已经存在，路径空间相对可控。

MSBuild 的职责更宽。对一个解决方案执行 `dotnet build` 时，它必须求值项目文件、导入的 `.props` 和 `.targets`、SDK 文件、包资产、目标框架、分析器配置、引用程序集、生成输出，以及输入/输出新鲜度。在真正编译之前，它会不断向文件系统提问：这个文件是否存在？哪个 target 生效？哪个引用胜出？这个输出是否过期？哪个包资产匹配当前框架？这就是为什么一个 .NET 构建即使最终产出的二进制只是触达路径的一小部分，也会产生大量 `CreateFile`、`QueryOpen`、`QueryDirectory` 和 `QueryNetworkOpenInformationFile`。

Roslyn 会放大 MSBuild 的自然形态，因为 Roslyn 本身就是一个编译器平台仓库。它包含 C# 和 VB 编译器、工作区、分析器、测试、源生成器、资源、`.editorconfig` 和 `.globalconfig` 输入、引用程序集使用，以及多目标输出。这不只是“编译很多 `.cs` 文件”，而是“求值一个大型 .NET 构建图、解析大量引用、运行编译器基础设施，并输出很多托管程序集”。

ripgrep 则放大原生编译器/链接器形态。Rust 编译会创建 crate 元数据和库（`.rmeta`、`.rlib`），从 Rustup 工具链库读取内容，在 Windows/MSVC 上写入 PDB 和原生库，并调用链接器/资源工具生成最终输出。这份跟踪与其说是路径扇出，不如说是集中式制品生产。

## 跟踪中到底有什么差异

这两份跟踪可以沿着一个轴清晰分开：路径扇出 vs. 制品集中。

Roslyn 触达约 15.38 万条唯一核心文件路径。ripgrep 约 3.4k。这个 45 倍差距不只是数字更大，它改变了安全问题本身。每一条不同路径，都是 AV 引擎判断哈希、签名者、包、位置或生成输出是否已知的又一次机会。随着路径集合爆炸，简单的热缓存假设会变得更弱。

第二个差异是元数据压力。Roslyn 有数百万次打开/查询操作，因为 MSBuild 和 .NET SDK 解析本质上是路径决策系统。构建系统必须先问文件系统很多问题，才知道要编译、复制、跳过或生成什么。ripgrep 也有元数据工作，但它不像 Roslyn 那样被构建图求值支配。

第三个差异是输出类型。ripgrep 创建的类可执行输出更少，但它们是被链接器、PDB、库文件等副作用包围的原生构建制品。Roslyn 则在 `artifacts\obj` 下创建大量新的 DLL。AV 引擎通常会把新创建的可执行内容、源文本和已知平台程序集区别对待，所以输出类别和数量一样重要。

第四个差异是信任形态。Roslyn 有大量可能已知的 Microsoft/SDK/引用程序集活动，但也有大量新的本地输出。ripgrep 的 Microsoft 输入较少，Rustup/Cargo 用户缓存活动占比更高，新鲜原生输出更少但更集中。这也是为什么只看 DLL 数量会误导：Microsoft 引用 DLL、NuGet 包 DLL 和本地新生成 DLL 不一定走同一条 AV 逻辑路径。

第五个差异是阶段形态：

| 阶段问题 | ripgrep 答案 | Roslyn 答案 |
| --- | --- | --- |
| 主要编排 | Cargo crate 图 | dotnet/MSBuild 解决方案图 |
| 编译器核心 | 多个 `rustc.exe` crate 编译 | `dotnet.exe` 加 `VBCSCompiler.exe` 编译器服务器读取 |
| 元数据压力 | 中等 | 很高 |
| 输出压力 | 集中的原生/PDB/lib/rmeta 输出 | 大量生成的托管 DLL/XML/cache/resources 输出 |
| 信任画像 | Rustup/Cargo 缓存 + 新鲜原生输出 | Microsoft SDK/NuGet/引用读取 + 新鲜 DLL 输出 |
| 网络/画像噪声 | 接近本地 | 观察到 restore/cache/类似遥测的网络活动 |

## 为什么 AV 产品会有不同反应

AV 产品不只是挂在文件读取上的字节扫描器。现代引擎通常会混合 minifilter 回调、路径策略、哈希缓存、签名者信誉、云端流行度、脚本规则、行为模型、可执行内容启发式、进程信誉，有时还包括针对开发工具的显式处理。ripgrep 和 Roslyn 会压到这些系统的不同组合。

这组测试分离出的主要 AV 决策点如下：

1. **每次打开和元数据查询的开销**

   Roslyn 是这里更敏锐的探针。如果一个产品给 `CreateFile`、`QueryOpen`、目录查询或元数据检查增加哪怕很小的延迟，Roslyn 也能把它放大数百万次。同一个产品在 ripgrep 上可能看起来没问题，因为 ripgrep 不会以同样方式在文件系统中扇出。

2. **新鲜 EXE/DLL 输出扫描**

   两个工作负载都会触发这一点，但制品不同。ripgrep 产出原生可执行文件、构建脚本制品和链接器制品。Roslyn 产出大量托管 DLL。会深度检查新创建可执行内容、等待云端信誉、或把“编译器生成了二进制”视为行为信号的产品，都可能拖慢任一工作负载。Roslyn 的总输出面更大；ripgrep 的原生链接器面更密集。

3. **签名者和路径信誉**

   Roslyn 读取更多可能属于 Microsoft/SDK/引用体系的内容。一个对 Microsoft 发布者/路径信誉处理很强的产品，可以让 Roslyn 输入侧的一大块走快速路径。但这不会自动让 Roslyn 变便宜，因为新鲜输出是本地新生成的。ripgrep 读取的 Microsoft 路径制品较少，Rustup/Cargo/用户缓存文件占比更高，所以它可能更依赖哈希/云端流行度，而不是 Microsoft Authenticode 快捷路径。

4. **包缓存信任**

   NuGet 和 Cargo/Rustup 缓存属于不同的信誉生态。NuGet 包可能常见，也可能有包签名，但其中的 DLL 不保证都有 Authenticode 签名。Rustup/Cargo 制品对开发者来说可能稳定常见，但更不可能落在 Microsoft 发布者/路径允许列表上。不同 AV 厂商在这里可能给出很不一样的判断。

5. **编译器服务器和长生命周期进程行为**

   Roslyn 的 `VBCSCompiler.exe` 会把大量编译器输入读取集中在一个编译器服务器进程里。有些产品会围绕进程身份和生命周期建立信誉与缓存决策。一个长生命周期编译器服务器，与许多短生命周期的 `rustc.exe` 进程不是同一种信号。

6. **FAST IO 回退和过滤驱动行为**

   Roslyn 有更多 `FAST IO DISALLOWED` 事件。这可能表示过滤器把文件系统推到了更慢的 IRP 路径。如果一个产品经常在元数据密集工作负载上阻止 fast I/O，Roslyn 会比 ripgrep 更清楚地暴露这种行为。

7. **云端缓存 vs. 本地缓存**

   First-cloud-seen 和平均运行回答的是不同安全问题。Roslyn 庞大的唯一路径集合会触发更多缓存和信誉决策。ripgrep 则可能由更少但更可疑的新鲜制品主导。一个本地哈希缓存很强、但云端信誉较慢的产品，可能表现出很高的首次运行惩罚，以及明显更好的平均表现。

8. **“开发工具”行为规则**

   编译器本来就会写入可执行代码，这正是它们让 AV 引擎难处理的原因。一些产品会特殊处理可信工具链；另一些则会把代码生成、链接器输出、未签名二进制或脚本生成模式当作值得关注的行为。ripgrep 测试原生编译器/链接器行为。Roslyn 测试大规模托管程序集生成。

这也是为什么两个图里的产品排名可能分叉。一个针对 Microsoft/.NET SDK 允许列表和低成本元数据 hook 做得很好的产品，可能在 Roslyn 上表现不错，直到生成 DLL 阶段开始产生代价。一个文件打开效率高、但对新原生可执行创建很激进的产品，可能扛住 Roslyn 的元数据压力，却在 ripgrep 的原生输出阶段失手。

## 跟踪规模

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

最具安全意义的一行是唯一核心文件路径。Roslyn 触达的唯一文件路径约为 ripgrep 的 45 倍。这就是跟踪开始像 AV 压力测试、而不只是大型构建的地方：路径越多，缓存未命中、元数据决策、信誉查询、哈希机会和过滤回调就越多。

## 操作族结构

| 操作族 | ripgrep 数量 | ripgrep % | Roslyn 数量 | Roslyn % |
| --- | ---: | ---: | ---: | ---: |
| file | 136,997 | 67.8% | 9,829,880 | 77.9% |
| registry | 51,132 | 25.3% | 1,342,861 | 10.6% |
| other | 10,430 | 5.2% | 1,388,455 | 11.0% |
| process/thread | 3,095 | 1.5% | 14,603 | 0.1% |
| profiling | 270 | 0.1% | 34,252 | 0.3% |
| network | 45 | 0.0% | 14,630 | 0.1% |

两个构建都由文件活动主导。Roslyn 更极端，因为文件图本身就是它的工作负载。ripgrep 的注册表占比仍然明显，因为原生 MSVC 路径会探测系统、SDK、Visual Studio 和运行时配置。Roslyn 的注册表活动在绝对值上也很多，但文件图太大，所以注册表只占较小一部分。

## 核心进程

| 进程 | ripgrep 事件 | 角色 |
| --- | ---: | --- |
| `rustc.exe` | 93,193 | Rust 编译器工作；读取 Rust 源码/元数据/库，并写入 crate 元数据/制品。 |
| `link.exe` | 32,788 | MSVC 链接器；写入 PDB/最终原生制品，并读取 Windows/MSVC 导入库。 |
| `VCTIP.EXE` | 25,858 | Visual Studio/MSVC 辅助活动，主要是注册表/工具链探测。 |
| `avbench.exe` | 23,617 | 基准测试 harness 和进程编排。 |
| `cargo.exe` | 21,983 | Cargo 包/构建图编排。 |

| 进程 | Roslyn 事件 | 角色 |
| --- | ---: | --- |
| `dotnet.exe` | 10,053,215 | .NET CLI/MSBuild 宿主；主导项目图、restore/构建元数据、SDK、NuGet 和输出活动。 |
| `VBCSCompiler.exe` | 2,436,411 | Roslyn 编译器服务器进程；大量读取源文件、引用、分析器和编译器输入。 |
| `avbench.exe` | 51,301 | 基准测试 harness 和进程编排。 |
| `Conhost.exe` | 40,145 | 命令行构建周边的控制台宿主活动。 |
| `VsdConfigTool.exe` | 37,482 | Visual Studio/dotnet 配置辅助活动，主要是注册表。 |

进程分布是最清晰的指纹之一。ripgrep 是围绕 Cargo、rustc、链接器和工具链辅助程序的小型进程生态。Roslyn 则压倒性地由 `dotnet.exe` 和编译器服务器构成。

## 文件操作画像

| 操作 | ripgrep | Roslyn | 解读 |
| --- | ---: | ---: | --- |
| `CreateFile` | 18,983 | 2,206,227 | Roslyn 打开了远多于 ripgrep 的文件和目录。 |
| `QueryOpen` | 3,257 | 1,615,022 | Roslyn 执行大量存在性/元数据检查。 |
| `QueryNetworkOpenInformationFile` | 647 | 1,050,266 | Roslyn 按路径大量查询文件元数据。 |
| `ReadFile` | 13,961 | 917,206 | Roslyn 读取更多源文件、程序集、配置和生成输入。 |
| `WriteFile` | 33,434 | 290,668 | Roslyn 绝对写入更多，但 ripgrep 按比例明显更偏写入。 |
| `QueryDirectory` | 1,041 | 195,120 | Roslyn 执行更广泛的目录/项目/包枚举。 |
| `Load Image` | 2,225 | 6,823 | 两者都会加载 EXE/DLL；这里已将其计为文件活动。 |
| `RegOpenKey` | 19,384 | 361,562 | 两者都会查询环境/工具链/运行时配置。 |
| `RegQueryValue` | 15,057 | 536,724 | Roslyn 在绝对值上有更多注册表值查询。 |

Roslyn 的 `CreateFile` 约为 ripgrep 的 116 倍，`QueryOpen` 约 496 倍，`QueryNetworkOpenInformationFile` 约 1623 倍。这是 MSBuild 构建图求值的典型特征：项目文件、导入的 `.props`/`.targets`、目标框架检查、引用程序集、包文件、生成输出和 up-to-date 决策。

ripgrep 的 `WriteFile` 事件只比 Roslyn 少约 8.7 倍，尽管核心事件总数少 63 倍。这就是信号所在：ripgrep 按比例明显更偏写入。这符合 Rust/原生构建形态：编译器/链接器输出、PDB、`.rlib`、`.rmeta`、`.lib`、`.o`、临时导入库和最终二进制。

## ripgrep 构建剖面

ripgrep 的核心活动集中在：

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

用一句话概括 ripgrep：它不会在几十万条路径上爆炸式扩散，而是把活动集中在 `target\release`、Rustup/Cargo 缓存、PDB、导入/静态库和原生链接器输出上。任何在制品创建、PDB 写入、链接器临时文件或新创建可执行/库内容上变贵的产品，都会在这里显形。

网络活动几乎可以忽略：45 个核心网络事件。这让 ripgrep 成为一个干净的、基本本地的编译工作负载。

## Roslyn 构建剖面

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

Roslyn 的跟踪像是在完整走过一个大型 .NET 生态：

- MSBuild 项目求值和 target 执行：`.csproj`、`.props`、`.targets`、`.editorconfig`、`.globalconfig`。
- 依赖和包基础设施：`.nuget`、`.sha512`、包缓存文件、生成的 NuGet props/targets。
- 编译器输入：`.cs`、`.vb`、`.resx`、引用程序集、分析器 DLL。
- 编译器/构建输出：`artifacts\obj`、生成的 `.cs`、输出 `.dll`、`.xml`、`.resources`、`.cache`、临时文件。
- 编译器服务器行为：非常大的 `VBCSCompiler.exe` 读取足迹，把编译器输入扫描集中在长生命周期进程中。

这种工作负载会惩罚弱文件元数据缓存或昂贵的打开/查询 hook。即使读取字节量并不总是很大，唯一路径和探测次数也已经非常庞大。

网络活动按比例仍然很小，但并非不存在：14,630 个核心网络事件。主要目标包括发往 Microsoft/Akamai 相关端点的 HTTPS 流量。结合命令和 NuGet/缓存路径，可能原因是 restore、SDK/工作负载元数据、包/缓存验证，或接近遥测的流量。如果目标是严格本地的 AV 测试，未来 Roslyn 运行应使用预先 restore 的包、`--no-restore`、禁用遥测，并最好加入断网对照。

## 注册表和配置行为

ripgrep 注册表活动按比例更高：占核心事件的 25.2%。热点根包括 `HKLM\System\CurrentControlSet`、`HKLM\SOFTWARE\Microsoft` 和 Visual Studio/MSVC 相关位置。这符合 Windows 原生构建路径：编译器发现、链接器设置、Windows SDK 查找、运行时配置和 Visual Studio 工具链管线。

Roslyn 的注册表活动绝对数量更大，但占比更小：134 万事件，占核心事件 10.6%。它主要集中在 `HKLM\System\CurrentControlSet` 以及 .NET/Visual Studio/SDK 查找。较小的百分比并不意味着注册表无关，而是文件图太大，使注册表退到了背景层。

对 AV 性能来说，注册表操作通常不如文件打开和写入重要。但注册表密集的设置阶段仍可能与自我保护、行为监控、策略检查和进程/工具链信誉相交。

## 错误和结果模式

两份跟踪都有大量非成功结果；这对构建系统来说很正常：

| 结果 | ripgrep | Roslyn | 解读 |
| --- | ---: | ---: | --- |
| `SUCCESS` | 149,553 | 9,374,926 | 已完成操作 |
| `NAME NOT FOUND` | 18,759 | 1,034,436 | 探测可选路径/文件/注册表键 |
| `FAST IO DISALLOWED` | 9,003 | 1,649,094 | Windows 回退到更慢的 IRP 路径；过滤器存在时常见 |
| `REPARSE` | 4,230 | 128,917 | 路径 reparse/符号链接/junction 行为 |
| `NO MORE FILES` | 347 | 70,608 | 目录枚举结束 |

`NAME NOT FOUND` 本身不是失败信号。构建系统会不断探测可选文件。Roslyn 让这一点特别明显，因为 MSBuild 和 NuGet 会评估大量可能的 import、目标框架、包资产、SDK 文件、生成输出和引用位置。

`FAST IO DISALLOWED` 值得关注，因为文件系统过滤器会影响 fast I/O 路径是否可用。Roslyn 的这类事件多得多，所以一个经常强制走慢路径的产品，会比在 ripgrep 上更容易伤到 Roslyn。

## 这些差异如何影响 AV 性能

### 签名/可信二进制信誉

ProcMon 不记录 Authenticode 签名者、catalog 签名、云端流行度，或 AV 产品内部的允许列表决策。因此，这份分析不能证明某个文件一定被信任。画像管线使用基于路径的信任/信誉桶作为代理指标。

信任故事并不是单向的：

| 问题 | 跟踪给出的可能答案 |
| --- | --- |
| 哪个工作负载在绝对值上触达更多可能属于 Microsoft/SDK/引用体系的二进制？ | Roslyn。它在 `C:\Program Files\dotnet`、引用程序集、SDK 位置和 Windows DLL 路径下有更多活动。 |
| 哪个工作负载加载更多平台可信 DLL？ | 绝对数量上是 Roslyn；两者的镜像加载主要都是 Windows/SDK 路径。 |
| 哪个工作负载创建更多新鲜的类可执行输出？ | Roslyn 多得多。它生成的 `artifacts\obj` DLL 写入主导类可执行写入。 |
| 哪个工作负载按比例更偏非 Microsoft/用户缓存/工具链形态？ | ripgrep。它规模更小，但大量活动来自 Rustup/Cargo 用户缓存和新鲜 `target\release` 输出。 |

增强画像给出的路径桶摘要：

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

这很重要，因为 Roslyn 巨大的 DLL/引用足迹并非成本均匀。很多输入可能稳定、已签名、常见，或来自包缓存，产品可能很快信任它们。但 Roslyn 也会创建大量新的 DLL，本地新输出未必能拿到同样的签名/云端/缓存快捷路径。ripgrep 文件更少、生成的类可执行写入也更少，但 Rustup/Cargo 缓存和新鲜原生制品未必会被 Microsoft Authenticode 允许列表覆盖。

### 数量、百分比、速率和加权压力

单看事件数不够。它回答的是“AV 有多少次介入机会？”，但无法回答“这些机会可能有多贵？”。百分比有助于理解形态，但会隐藏规模。因此管线报告四层信息：

| 层级 | 回答什么问题 | 为什么重要 |
| --- | --- | --- |
| 绝对数量 | AV 暴露面有多大 | Roslyn 有更多文件打开、元数据查询、失败探测和写入 |
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

这个分数是启发式的，不是实测延迟。它使用透明权重：元数据查询较低，读取较低/中等，写入更高，镜像加载更高，类可执行写入很高。可能属于 Microsoft OS/SDK 的路径使用较低乘数；新鲜构建输出和用户/包/工具链路径使用更高乘数。

这会改变结论。Roslyn 总 AV 压力压倒性更高，因为它规模巨大，并且创建了大量新鲜 DLL 输出。ripgrep 每 1,000 事件的压力密度相近且略高，因为它较小的事件流集中在写入和新鲜原生制品上。Roslyn 是更大的总压力测试；ripgrep 是更密集的原生制品压力测试。

未来运行中应继续跟踪的类似信誉修饰因素：

- Authenticode 签名者和 catalog 签名：Microsoft 签名的 OS/SDK/引用二进制可能比未签名本地输出走更快的 AV 路径。
- 云端流行度和文件年龄：常见 SDK/NuGet/Rustup 文件可能已知；新构建输出则不是。
- 路径和发布者信任：`C:\Windows`、`Program Files\dotnet`、Visual Studio 和 Windows Kits 往往不同于 `C:\bench` 或用户配置文件缓存。
- 生成的类可执行输出：新的 `.exe`/`.dll` 文件可能触发更深的静态、行为或信誉检查。
- 包缓存信任：NuGet 包可能有包签名或很常见，但其中 DLL 不总是 Authenticode 签名。
- 工具链缓存信任：Rustup/Cargo 制品可能稳定常见，但不是 Microsoft 签名。
- 镜像加载 vs. 文件读取 vs. 文件写入：AV 产品可能区别对待“加载 DLL”、“把 DLL 当数据读取”和“写入新的 DLL”。
- 脚本和 MOTW 语义：`.ps1`、`.js`、下载文件和 Zone.Identifier/MOTW 可能显著改变信誉路径。
- 缓存范围：产品本地缓存、云端缓存、文件哈希缓存和 VM 重置行为，会让 first-cloud-seen 与平均运行结果分叉。

### Ripgrep / Rust 原生构建

主要 AV 压力：

- 扫描新写入的编译器/链接器制品。
- 扫描 PDB、`.lib`、`.rmeta`、`.rlib`、`.o`、`.a` 和最终可执行输出。
- 拦截 `%TEMP%` 下的链接器临时文件。
- 反复读取 Rust 工具链库和 Rust 标准库制品。
- 原生 Windows SDK/MSVC 工具链发现和注册表探测。

预期的 AV 敏感行为：

- 如果产品扫描每一个新创建的对象/库/调试制品，影响可能较高。
- 如果产品对 Rust 工具链库有良好的路径/哈希缓存，云端/缓存预热后应明显改善。
- 如果产品把编译器/链接器输出视为可疑的可执行生成行为，即使唯一路径较少，也可能增加延迟。

### Roslyn / .NET 托管构建

主要 AV 压力：

- 横跨源文件、SDK、NuGet、引用程序集、生成文件和输出文件的大量文件打开/查询/读取。
- 大量 `.dll` 读取和写入，包括分析器、引用程序集、编译器/运行时组件和输出。
- 通过 `.props` 和 `.targets` 执行 MSBuild/NuGet target 与属性求值。
- 通过路径存在性、时间戳、元数据和目录枚举执行增量构建和依赖图检查。
- 编译器服务器进程读取大量编译器输入。

预期的 AV 敏感行为：

- 如果产品的每次打开或每次查询过滤较昂贵，即使字节扫描不多，也可能受影响。
- 如果产品在 VM 重置后的缓存复用较弱，会反复为 SDK、NuGet、引用程序集和分析器路径付费。
- 如果产品对 DLL、源生成器、分析器或新生成程序集应用更重规则，可能出现明显影响。
- 如果产品对大量不同 DLL/源文件/生成路径执行云端信誉检查，first-cloud-seen 影响可能很高。

## 证据矩阵

下面是上述论点的压缩版：

| 维度 | ripgrep | Roslyn |
| --- | --- | --- |
| 生态 | Rust/Cargo/原生 MSVC 链接 | .NET/MSBuild/Roslyn/NuGet |
| 规模 | 紧凑 | 很大 |
| 唯一文件 | 数千级 | 十万级 |
| 主要压力 | 制品写入和链接器/编译器输出 | 文件图遍历、元数据检查、引用、包 |
| 主要进程形态 | `cargo` + `rustc` + `link` | `dotnet` + `VBCSCompiler` |
| AV 缓存问题 | 产品能否高效处理生成的原生制品？ | 产品能否高效处理庞大的托管图遍历和生成 DLL？ |
| 网络敏感性 | 接近本地 | 观察到一些 restore/元数据/类似遥测的 HTTPS 活动 |

价值在于对比。把它们当作“编译时间”的重复样本，会丢掉真正的信号。

## 后续基准解读建议

1. 保留 ripgrep 和 Roslyn 的独立图表，不要只保留一个合并的“编译”结果。它们的 OS 操作结构差异足够大，单一合并数字会掩盖有用的产品行为。

2. 在构建总体分数时，把它们视为两个编译子工作负载，而不是重复样本。一个产品可以在 ripgrep 上很好、在 Roslyn 上很差，或者反过来，而且这背后可以有合理技术原因。

3. 对 Roslyn，如果目标是纯本地 AV 开销，应增加严格离线/预 restore 变体。当前跟踪包含一些网络活动，而 `dotnet build` 可能隐式 restore 或触碰工作负载/包元数据。

4. 对两个工作负载，都应分别报告 first-cloud-seen 和平均/缓存预热行为。first-cloud-seen 捕获云端信誉和产品冷缓存行为；平均运行捕获信誉/缓存有机会稳定后的日常开发体验。

5. 如果某个产品在 Roslyn 上是离群值，应检查文件打开/查询延迟，而不只是写入扫描。Roslyn 更受路径和元数据行为支配，而不是简单的“写入字节数”模型。

6. 如果某个产品在 ripgrep 上是离群值，应检查新创建的编译器/链接器制品、PDB 写入和临时导入库行为。

## 限制

ProcMon 跟踪是观察性的。它显示发生了哪些操作，但不能精确说明每个 AV 产品给每个操作增加了多少延迟。数量仍然重要，因为它显示暴露给文件系统过滤器的表面积；但要做时延归因，需要每个产品的时序数据，或 ETW/WPA 这类延迟分析。

核心构建进程过滤基于 ProcMon 的进程创建关系。这比静态按进程名过滤更强，但并不完美：它会包括 `Conhost.exe` 这类控制台/辅助子进程，也可能漏掉不是基准进程后代、但执行了构建相关工作的长期系统服务。

生成 JSON 中的字节计数应视为方向性指标。ProcMon 的 `Detail` 字段会随操作变化；本分析关注的是操作结构、路径扇出、进程组合、文件类型组合和信誉表面积，而不是精确的字节核算。

## 结论

ripgrep 是紧凑的原生制品压力测试。Roslyn 是大型托管图压力测试。它们都是编译工作负载，但提出的是不同的 AV 问题。这正是两者都应该留在测试套件中的原因。
