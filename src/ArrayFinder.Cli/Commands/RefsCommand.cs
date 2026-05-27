using ArrayFinder.Cli.Reporting;
using ArrayFinder.Core.Analysis;
using ArrayFinder.Core.Models;
using ArrayFinder.Core.Reporting;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace ArrayFinder.Cli.Commands;

internal sealed class RefsCommand : AsyncCommand<RefsCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[PATH]")]
        [Description(".sln または .csproj ファイルのパス（省略時はカレントディレクトリを検索）")]
        public string? Path { get; init; }

        [CommandOption("-k|--kind")]
        [DefaultValue("field,property")]
        [Description("対象の宣言種別（カンマ区切り）: field, property, method, ctor, local, param, type, event, all")]
        public string Kind { get; init; } = "field,property";

        [CommandOption("--zero-refs")]
        [DefaultValue(false)]
        [Description("参照数ゼロの宣言のみ表示する")]
        public bool ZeroRefsOnly { get; init; }

        [CommandOption("--min-refs")]
        [Description("この参照数以下の宣言のみ表示する（例: --min-refs 2）")]
        public int? MinRefs { get; init; }

        [CommandOption("--path-include")]
        [Description("含めるファイルパスのパターン（カンマ区切り。部分一致 or glob）")]
        public string? PathInclude { get; init; }

        [CommandOption("--path-exclude")]
        [Description("除外するファイルパスのパターン（カンマ区切り。例: tests/**,obj/）")]
        public string? PathExclude { get; init; }

        [CommandOption("-f|--format")]
        [DefaultValue("console")]
        [Description("出力形式: console, json, csv, markdown")]
        public string Format { get; init; } = "console";

        [CommandOption("-o|--output")]
        [Description("出力ファイルパス（省略時は標準出力）")]
        public string? Output { get; init; }

        [CommandOption("--no-snippet")]
        [DefaultValue(false)]
        [Description("ソースコードスニペットを出力しない")]
        public bool NoSnippet { get; init; }

        [CommandOption("--sort")]
        [DefaultValue("refs")]
        [Description("ソート順: refs（参照数昇順）, file, kind, name")]
        public string Sort { get; init; } = "refs";
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

        var kinds = ParseKinds(settings.Kind);
        if (kinds.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]エラー: 有効な宣言種別が指定されていません。[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[bold]対象種別:[/] {string.Join(", ", kinds)}");

        var options = new RefsOptions
        {
            Kinds = kinds,
            IncludeSnippets = !settings.NoSnippet,
            IncludePathPatterns = SplitPatterns(settings.PathInclude),
            ExcludePathPatterns = SplitPatterns(settings.PathExclude),
        };

        var analyzer = new RefsAnalyzer(options);
        RefsAnalysisResult result = null!;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("解析中...", async ctx =>
            {
                var progress = new Progress<string>(msg => ctx.Status(Markup.Escape(msg)));
                result = await analyzer.AnalyzeAsync(path, progress);
            });

        if (result.Warnings.Count > 0)
        {
            foreach (var w in result.Warnings)
                AnsiConsole.MarkupLine($"[yellow]警告: {Markup.Escape(w)}[/]");
        }

        var declarations = ApplyFilter(ApplySort(result.Declarations, settings.Sort), settings);
        var reporter = CreateReporter(settings.Format);

        if (settings.Output is { } outputPath)
        {
            await using var writer = new StreamWriter(outputPath, append: false);
            await reporter.WriteAsync(declarations, writer);
            AnsiConsole.MarkupLine($"[green]出力完了:[/] {Markup.Escape(outputPath)}");
        }
        else
        {
            if (settings.Format == "console")
            {
                await reporter.WriteAsync(declarations, Console.Out);
            }
            else
            {
                var sw = new StringWriter();
                await reporter.WriteAsync(declarations, sw);
                Console.Write(sw.ToString());
            }
        }

        return 0;
    }

    private static HashSet<DeclarationKind> ParseKinds(string kindArg)
    {
        var all = new HashSet<DeclarationKind>(Enum.GetValues<DeclarationKind>());
        var set = new HashSet<DeclarationKind>();

        foreach (var token in kindArg.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = token.Trim().ToLowerInvariant();
            switch (t)
            {
                case "all": return all;
                case "field": set.Add(DeclarationKind.Field); break;
                case "property" or "prop": set.Add(DeclarationKind.Property); break;
                case "method": set.Add(DeclarationKind.Method); break;
                case "ctor" or "constructor": set.Add(DeclarationKind.Constructor); break;
                case "local": set.Add(DeclarationKind.Local); break;
                case "param" or "parameter": set.Add(DeclarationKind.Parameter); break;
                case "type": set.Add(DeclarationKind.Type); break;
                case "event": set.Add(DeclarationKind.Event); break;
            }
        }
        return set;
    }

    private static IReadOnlyList<SymbolDeclarationInfo> ApplySort(
        IReadOnlyList<SymbolDeclarationInfo> items, string sort) =>
        sort.ToLowerInvariant() switch
        {
            "file" => items.OrderBy(d => d.FilePath).ThenBy(d => d.Line).ToList(),
            "kind" => items.OrderBy(d => d.Kind).ThenBy(d => d.ReferenceCount).ThenBy(d => d.Name).ToList(),
            "name" => items.OrderBy(d => d.Name).ThenBy(d => d.FilePath).ToList(),
            _ => items.OrderBy(d => d.ReferenceCount).ThenBy(d => d.Kind).ThenBy(d => d.FilePath).ThenBy(d => d.Line).ToList(),
        };

    private static IReadOnlyList<SymbolDeclarationInfo> ApplyFilter(
        IReadOnlyList<SymbolDeclarationInfo> items, Settings settings)
    {
        if (settings.ZeroRefsOnly)
            items = items.Where(d => d.ReferenceCount == 0).ToList();
        else if (settings.MinRefs.HasValue)
            items = items.Where(d => d.ReferenceCount <= settings.MinRefs.Value).ToList();
        return items;
    }

    private static IReadOnlyList<string>? SplitPatterns(string? value) =>
        value?.Split(',', StringSplitOptions.RemoveEmptyEntries)
              .Select(s => s.Trim())
              .Where(s => s.Length > 0)
              .ToList() is { Count: > 0 } list ? list : null;

    private static string? ResolvePath(string? path)
    {
        if (path is not null)
            return File.Exists(path) ? path : null;

        var dir = Directory.GetCurrentDirectory();
        var sln = Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (sln is not null) return sln;
        return Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
    }

    private static ISymbolReporter CreateReporter(string format) => format.ToLowerInvariant() switch
    {
        "json" => new SymbolJsonReporter(),
        "csv" => new SymbolCsvReporter(),
        "markdown" or "md" => new SymbolMarkdownReporter(),
        _ => new SymbolConsoleReporter(),
    };
}
