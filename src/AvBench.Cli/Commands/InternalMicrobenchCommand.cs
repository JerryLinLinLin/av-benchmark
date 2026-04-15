using System.CommandLine;
using System.Runtime.Versioning;
using System.Text.Json;
using AvBench.Core.Microbench;
using AvBench.Core.Serialization;

namespace AvBench.Cli.Commands;

[SupportedOSPlatform("windows")]
public static class InternalMicrobenchCommand
{
    public static Command Create()
    {
        var scenarioOption = new Option<string>("--scenario")
        {
            Description = "Internal microbenchmark scenario id."
        };
        scenarioOption.Required = true;

        var rootOption = new Option<DirectoryInfo>("--root")
        {
            Description = "Working directory for the internal microbenchmark."
        };
        rootOption.Required = true;

        var operationsOption = new Option<int>("--operations")
        {
            Description = "Total operations or iterations for the scenario.",
            DefaultValueFactory = _ => 0
        };

        var batchSizeOption = new Option<int>("--batch-size")
        {
            Description = "Batch size for file-create-delete.",
            DefaultValueFactory = _ => 100
        };

        var extensionOption = new Option<string?>("--extension")
        {
            Description = "Extension used by ext-sensitivity scenarios."
        };

        var zipPathOption = new Option<FileInfo?>("--zip-path")
        {
            Description = "Path to the prepared archive zip for archive-extract."
        };

        var unsignedExeOption = new Option<FileInfo?>("--unsigned-exe")
        {
            Description = "Path to the prepared unsigned executable."
        };

        var iterationsOption = new Option<int>("--iterations")
        {
            Description = "Archive extract iterations.",
            DefaultValueFactory = _ => 0
        };

        var applyMotwOption = new Option<bool>("--apply-motw")
        {
            Description = "Apply Mark of the Web before execution.",
            DefaultValueFactory = _ => false
        };

        var command = new Command("internal-microbench")
        {
            Description = "Internal worker for API microbenchmarks.",
            Hidden = true
        };

        command.Options.Add(scenarioOption);
        command.Options.Add(rootOption);
        command.Options.Add(operationsOption);
        command.Options.Add(batchSizeOption);
        command.Options.Add(extensionOption);
        command.Options.Add(zipPathOption);
        command.Options.Add(unsignedExeOption);
        command.Options.Add(iterationsOption);
        command.Options.Add(applyMotwOption);

        command.SetAction(parseResult =>
        {
            var request = new MicrobenchRequest
            {
                ScenarioId = parseResult.GetValue(scenarioOption)!,
                RootPath = parseResult.GetValue(rootOption)!.FullName,
                Operations = parseResult.GetValue(operationsOption),
                BatchSize = parseResult.GetValue(batchSizeOption),
                Extension = parseResult.GetValue(extensionOption),
                ZipPath = parseResult.GetValue(zipPathOption)?.FullName,
                UnsignedExePath = parseResult.GetValue(unsignedExeOption)?.FullName,
                Iterations = parseResult.GetValue(iterationsOption),
                ApplyMotw = parseResult.GetValue(applyMotwOption)
            };

            var metrics = MicrobenchWorker.Execute(request);
            Console.Out.Write(JsonSerializer.Serialize(metrics, AvBenchJsonContext.Default.MicrobenchMetrics));
            return 0;
        });

        return command;
    }
}
