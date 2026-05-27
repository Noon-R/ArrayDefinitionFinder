using ArrayFinder.Core.Models;
using ArrayFinder.Core.Reporting;
using Spectre.Console;

namespace ArrayFinder.Cli.Reporting;

public sealed class ConsoleReporter : IReporter
{
    public Task WriteAsync(IReadOnlyList<ArrayUsageInfo> usages, TextWriter writer, CancellationToken ct = default)
    {
        if (usages.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]配列使用箇所が見つかりませんでした。[/]");
            return Task.CompletedTask;
        }

        // サマリー
        AnsiConsole.WriteLine();
        var summaryTable = new Table()
            .Title("[bold cyan]配列使用型サマリー[/]")
            .AddColumn("Element Type")
            .AddColumn(new TableColumn("Count").Centered())
            .AddColumn(new TableColumn("TypeDecl").Centered())
            .AddColumn(new TableColumn("Creation").Centered())
            .AddColumn(new TableColumn("Implicit").Centered())
            .AddColumn(new TableColumn("Collection").Centered())
            .AddColumn(new TableColumn("Method").Centered())
            .Border(TableBorder.Rounded);

        foreach (var g in usages.GroupBy(u => u.ElementType).OrderByDescending(g => g.Count()))
        {
            ct.ThrowIfCancellationRequested();
            summaryTable.AddRow(
                $"[green]{Markup.Escape(g.Key)}[/]",
                $"[bold]{g.Count()}[/]",
                Count(g, ArrayKind.TypeDeclaration),
                Count(g, ArrayKind.ArrayCreation),
                Count(g, ArrayKind.ImplicitCreation),
                Count(g, ArrayKind.CollectionExpression),
                Count(g, ArrayKind.MethodReturn));
        }
        AnsiConsole.Write(summaryTable);

        // 全件テーブル
        var hasRefCounts = usages.Any(u => u.ReferenceCount.HasValue);
        AnsiConsole.WriteLine();
        var detailTable = new Table()
            .Title($"[bold cyan]配列使用一覧 ({usages.Count} 件)[/]")
            .AddColumn("Element Type")
            .AddColumn("Kind")
            .AddColumn(new TableColumn("Rk").Centered())
            .AddColumn("File")
            .AddColumn(new TableColumn("Line").RightAligned())
            .AddColumn("Type::Member")
            .AddColumn("Snippet")
            .Border(TableBorder.Simple);

        if (hasRefCounts)
            detailTable.AddColumn(new TableColumn("Refs").RightAligned());

        foreach (var u in usages)
        {
            ct.ThrowIfCancellationRequested();
            var kindMarkup = u.Kind switch
            {
                ArrayKind.TypeDeclaration => "[blue]TypeDecl[/]",
                ArrayKind.ArrayCreation => "[cyan]Creation[/]",
                ArrayKind.ImplicitCreation => "[magenta]Implicit[/]",
                ArrayKind.CollectionExpression => "[yellow]Collect[/]",
                ArrayKind.MethodReturn => "[green]Method[/]",
                _ => u.Kind.ToString(),
            };

            var fileName = Path.GetFileName(u.FilePath);
            var member = string.IsNullOrEmpty(u.ContainingType)
                ? u.ContainingMember
                : $"{u.ContainingType}::{u.ContainingMember}";
            var methodHint = u.MethodName is { } m ? $" ({m})" : "";
            var snippet = u.SourceSnippet is { } s
                ? Markup.Escape(s.Length > 50 ? s[..47] + "..." : s)
                : "";

            var cells = new List<string>
            {
                $"[green]{Markup.Escape(u.ElementType)}[/]",
                kindMarkup + Markup.Escape(methodHint),
                u.Rank.ToString(),
                Markup.Escape(fileName),
                $"[grey]{u.Line}:{u.Column}[/]",
                Markup.Escape(member),
                $"[grey]{snippet}[/]",
            };

            if (hasRefCounts)
            {
                cells.Add(u.ReferenceCount switch
                {
                    null => "[grey]-[/]",
                    0 => "[red bold]0[/]",
                    int n => n.ToString(),
                });
            }

            detailTable.AddRow(cells.ToArray());
        }
        AnsiConsole.Write(detailTable);

        AnsiConsole.MarkupLine($"\n[bold]合計: [green]{usages.Count}[/] 件[/]");
        return Task.CompletedTask;
    }

    private static string Count(IGrouping<string, ArrayUsageInfo> g, ArrayKind kind)
    {
        var c = g.Count(u => u.Kind == kind);
        return c > 0 ? c.ToString() : "[grey]-[/]";
    }
}
