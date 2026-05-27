using ArrayFinder.Cli.Commands;
using Microsoft.Build.Locator;
using Spectre.Console.Cli;

// MSBuildLocator はあらゆる Roslyn アセンブリが JIT される前に呼ぶ必要がある
MSBuildLocator.RegisterDefaults();

await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    var app = new CommandApp();
    app.Configure(cfg =>
    {
        cfg.SetApplicationName("arrayfinder");
        cfg.SetApplicationVersion("1.0.0");

        cfg.AddCommand<AnalyzeCommand>("analyze")
           .WithAlias("a")
           .WithDescription("配列の定義・使用箇所を静的解析で一覧化する")
           .WithExample("analyze", "MyApp.sln")
           .WithExample("analyze", "MyApp.csproj", "--format", "json", "--output", "report.json")
           .WithExample("analyze", "MyApp.sln", "--filter-type", "int,string", "--sort", "type")
           .WithExample("analyze", "MyApp.sln", "--zero-refs")
           .WithExample("analyze", "MyApp.sln", "--path-exclude", "tests/**,obj/");

        cfg.AddCommand<RefsCommand>("refs")
           .WithAlias("r")
           .WithDescription("任意のシンボル宣言（フィールド/プロパティ/メソッドなど）の参照数を計上し、未参照を検出する")
           .WithExample("refs", "MyApp.sln")
           .WithExample("refs", "MyApp.sln", "--kind", "field,property", "--zero-refs")
           .WithExample("refs", "MyApp.sln", "--kind", "method", "--min-refs", "1")
           .WithExample("refs", "MyApp.sln", "--kind", "all", "--path-include", "src/");
    });
    return await app.RunAsync(args);
}
