using ArrayFinder.Cli.Reporting;
using ArrayFinder.Core.Analysis;
using ArrayFinder.Core.Models;
using ArrayFinder.Core.Reporting;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace ArrayFinder.Cli.Commands;

internal sealed class AnalyzeCommand : AsyncCommand<AnalyzeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[PATH]")]
        [Description(".sln または .csproj ファイルのパス（省略時はカレントディレクトリを検索）")]
        public string? Path { get; init; }

        [CommandOption("-f|--format")]
        [DefaultValue("console")]
        [Description("出力形式: console, json, csv, markdown")]
        public string Format { get; init; } = "console";

        [CommandOption("-o|--output")]
        [Description("出力ファイルパス（省略時は標準出力）")]
        public string? Output { get; init; }

        [CommandOption("--no-linq")]
        [DefaultValue(false)]
        [Description("LINQ/メソッド戻り値による配列生成を除外する")]
        public bool NoLinq { get; init; }

        [CommandOption("--no-snippet")]
        [DefaultValue(false)]
        [Description("ソースコードスニペットを出力しない")]
        public bool NoSnippet { get; init; }

        [CommandOption("--filter-type")]
        [Description("要素型でフィルタリング（カンマ区切り。例: int,string,MyDto）")]
        public string? FilterType { get; init; }

        [CommandOption("--sort")]
        [DefaultValue("file")]
        [Description("ソート順: file, type, kind")]
        public string Sort { get; init; } = "file";
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var path = ResolvePath(settings.Path);
        if (path is null)
        {
            AnsiConsole.MarkupLine("[red]エラー: .sln または .csproj ファイルが見つかりません。[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[bold]解析対象:[/] {Markup.Escape(path)}");

        var options = new AnalysisOptions
        {
            IncludeLinqAndMethodReturns = !settings.NoLinq,
            IncludeSnippets = !settings.NoSnippet,
            FilterElementTypes = settings.FilterType?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                      .Select(s => s.Trim())
                                                      .ToList(),
        };

        var analyzer = new ProjectAnalyzer(options);
        AnalysisResult result = null!;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("解析中...", async ctx =>
            {
                var progress = new Progress<string>(msg =>
                {
                    ctx.Status(Markup.Escape(msg));
                });
                result = await analyzer.AnalyzeAsync(path, progress);
            });

        if (result.Warnings.Count > 0)
        {
            foreach (var w in result.Warnings)
                AnsiConsole.MarkupLine($"[yellow]警告: {Markup.Escape(w)}[/]");
        }

        var usages = ApplySort(result.Usages, settings.Sort);
        var reporter = CreateReporter(settings.Format);

        if (settings.Output is { } outputPath)
        {
            await using var writer = new StreamWriter(outputPath, append: false);
            await reporter.WriteAsync(usages, writer);
            AnsiConsole.MarkupLine($"[green]出力完了:[/] {Markup.Escape(outputPath)}");
        }
        else
        {
            if (settings.Format == "console")
            {
                await reporter.WriteAsync(usages, Console.Out);
            }
            else
            {
                // json/csv/markdown をコンソール出力
                var sw = new StringWriter();
                await reporter.WriteAsync(usages, sw);
                Console.Write(sw.ToString());
            }
        }

        return 0;
    }

    private static string? ResolvePath(string? path)
    {
        if (path is not null)
            return File.Exists(path) ? path : null;

        var dir = Directory.GetCurrentDirectory();
        var sln = Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (sln is not null) return sln;

        return Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
    }

    private static IReadOnlyList<ArrayUsageInfo> ApplySort(IReadOnlyList<ArrayUsageInfo> usages, string sort)
    {
        return sort.ToLowerInvariant() switch
        {
            "type" => usages.OrderBy(u => u.ElementType).ThenBy(u => u.FilePath).ThenBy(u => u.Line).ToList(),
            "kind" => usages.OrderBy(u => u.Kind).ThenBy(u => u.ElementType).ThenBy(u => u.FilePath).ToList(),
            _ => usages.OrderBy(u => u.FilePath).ThenBy(u => u.Line).ToList(), // "file"
        };
    }

    private static IReporter CreateReporter(string format) => format.ToLowerInvariant() switch
    {
        "json" => new JsonReporter(),
        "csv" => new CsvReporter(),
        "markdown" or "md" => new MarkdownReporter(),
        _ => new ConsoleReporter(),
    };
}
