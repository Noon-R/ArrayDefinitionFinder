using ArrayFinder.Core.Models;

namespace ArrayFinder.Core.Reporting;

public sealed class MarkdownReporter : IReporter
{
    public async Task WriteAsync(IReadOnlyList<ArrayUsageInfo> usages, TextWriter writer, CancellationToken ct = default)
    {
        await writer.WriteLineAsync($"# Array Usage Report");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}  ");
        await writer.WriteLineAsync($"Total: **{usages.Count}** usages");
        await writer.WriteLineAsync();

        // サマリーテーブル
        await writer.WriteLineAsync("## Summary by Element Type");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("| Element Type | Count |");
        await writer.WriteLineAsync("|---|---|");
        foreach (var g in usages.GroupBy(u => u.ElementType).OrderByDescending(g => g.Count()))
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync($"| `{g.Key}` | {g.Count()} |");
        }

        await writer.WriteLineAsync();

        // 全件テーブル
        await writer.WriteLineAsync("## All Usages");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("| Element Type | Kind | Rank | File | Line | Containing Type | Member | Snippet | Refs |");
        await writer.WriteLineAsync("|---|---|---|---|---|---|---|---|---|");

        foreach (var u in usages)
        {
            ct.ThrowIfCancellationRequested();
            var file = Path.GetFileName(u.FilePath);
            var snippet = u.SourceSnippet is { } s ? $"`{s.Replace("`", "'")}`" : "";
            var refs = u.ReferenceCount?.ToString() ?? "";
            await writer.WriteLineAsync(
                $"| `{u.ElementType}` | {u.Kind} | {u.Rank} | {file} | {u.Line} | {u.ContainingType} | {u.ContainingMember} | {snippet} | {refs} |");
        }
    }
}
