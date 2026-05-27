using ArrayFinder.Core.Models;
using ArrayFinder.Core.Reporting;
using Spectre.Console;

namespace ArrayFinder.Cli.Reporting;

public sealed class SymbolConsoleReporter : ISymbolReporter
{
    public Task WriteAsync(IReadOnlyList<SymbolDeclarationInfo> declarations, TextWriter writer, CancellationToken ct = default)
    {
        if (declarations.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]宣言が見つかりませんでした。[/]");
            return Task.CompletedTask;
        }

        // サマリー
        AnsiConsole.WriteLine();
        var summaryTable = new Table()
            .Title("[bold cyan]宣言種別サマリー[/]")
            .AddColumn("Kind")
            .AddColumn(new TableColumn("Count").Centered())
            .AddColumn(new TableColumn("Zero-Refs").Centered())
            .Border(TableBorder.Rounded);

        foreach (var g in declarations.GroupBy(d => d.Kind).OrderBy(g => g.Key))
        {
            ct.ThrowIfCancellationRequested();
            var zeroCount = g.Count(d => d.ReferenceCount == 0);
            var zeroMarkup = zeroCount > 0 ? $"[red]{zeroCount}[/]" : "[grey]0[/]";
            summaryTable.AddRow(g.Key.ToString(), g.Count().ToString(), zeroMarkup);
        }
        AnsiConsole.Write(summaryTable);

        // 全件テーブル
        AnsiConsole.WriteLine();
        var detailTable = new Table()
            .Title($"[bold cyan]宣言一覧 ({declarations.Count} 件)[/]")
            .AddColumn("Name")
            .AddColumn("Type")
            .AddColumn("Kind")
            .AddColumn("File")
            .AddColumn(new TableColumn("Line").RightAligned())
            .AddColumn("Containing")
            .AddColumn(new TableColumn("Refs").RightAligned())
            .AddColumn("Snippet")
            .Border(TableBorder.Simple);

        foreach (var d in declarations)
        {
            ct.ThrowIfCancellationRequested();

            var kindMarkup = d.Kind switch
            {
                DeclarationKind.Field => "[blue]Field[/]",
                DeclarationKind.Property => "[cyan]Prop[/]",
                DeclarationKind.Method => "[magenta]Method[/]",
                DeclarationKind.Constructor => "[magenta].ctor[/]",
                DeclarationKind.Local => "[grey]Local[/]",
                DeclarationKind.Parameter => "[grey]Param[/]",
                DeclarationKind.Type => "[yellow]Type[/]",
                DeclarationKind.Event => "[green]Event[/]",
                _ => d.Kind.ToString(),
            };

            var refsMarkup = d.ReferenceCount == 0
                ? "[red bold]0[/]"
                : d.ReferenceCount.ToString();

            var context = string.IsNullOrEmpty(d.ContainingMember)
                ? d.ContainingType
                : $"{d.ContainingType}::{d.ContainingMember}";

            var snippet = d.SourceSnippet is { } s
                ? Markup.Escape(s.Length > 45 ? s[..42] + "..." : s)
                : "";

            detailTable.AddRow(
                $"[bold]{Markup.Escape(d.Name)}[/]",
                Markup.Escape(d.TypeName.Length > 30 ? d.TypeName[..27] + "..." : d.TypeName),
                kindMarkup,
                Markup.Escape(Path.GetFileName(d.FilePath)),
                $"[grey]{d.Line}[/]",
                Markup.Escape(context),
                refsMarkup,
                $"[grey]{snippet}[/]");
        }
        AnsiConsole.Write(detailTable);

        var zeroTotal = declarations.Count(d => d.ReferenceCount == 0);
        AnsiConsole.MarkupLine($"\n[bold]合計: [green]{declarations.Count}[/] 件 / 参照数ゼロ: [red]{zeroTotal}[/] 件[/]");
        return Task.CompletedTask;
    }
}
