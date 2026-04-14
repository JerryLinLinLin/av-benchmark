using AvBench.Compare;

var rootCommand = CompareCommand.Create();

return rootCommand.Parse(args).Invoke();
