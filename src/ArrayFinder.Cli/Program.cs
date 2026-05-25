using ArrayFinder.Cli.Commands;
using Microsoft.Build.Locator;
using Spectre.Console.Cli;

// MSBuildLocator はあらゆる Roslyn アセンブリが JIT される前に呼ぶ必要がある
MSBuildLocator.RegisterDefaults();

await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    var app = new CommandApp<AnalyzeCommand>();
    app.Configure(cfg =>
    {
        cfg.SetApplicationName("arrayfinder");
        cfg.SetApplicationVersion("1.0.0");
        cfg.AddExample("MyApp.sln");
        cfg.AddExample("MyApp.csproj", "--format", "json", "--output", "report.json");
        cfg.AddExample("MyApp.sln", "--filter-type", "int,string", "--sort", "type");
        cfg.AddExample("MyApp.sln", "--no-linq", "--format", "markdown", "--output", "report.md");
    });
    return await app.RunAsync(args);
}
