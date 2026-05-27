using ArrayFinder.Core.Models;

namespace ArrayFinder.Core.Reporting;

public sealed class SymbolMarkdownReporter : ISymbolReporter
{
    public async Task WriteAsync(IReadOnlyList<SymbolDeclarationInfo> declarations, TextWriter writer, CancellationToken ct = default)
    {
        await writer.WriteLineAsync("# Symbol Reference Report");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}  ");
        await writer.WriteLineAsync($"Total: **{declarations.Count}** declarations, " +
                                    $"**{declarations.Count(d => d.ReferenceCount == 0)}** zero-refs");
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("## Summary by Kind");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("| Kind | Count | Zero-Refs |");
        await writer.WriteLineAsync("|---|---|---|");
        foreach (var g in declarations.GroupBy(d => d.Kind).OrderBy(g => g.Key))
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync($"| {g.Key} | {g.Count()} | {g.Count(d => d.ReferenceCount == 0)} |");
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## Declarations");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("| Name | Type | Kind | File | Line | Containing Type | Member | Refs | Snippet |");
        await writer.WriteLineAsync("|---|---|---|---|---|---|---|---|---|");

        foreach (var d in declarations)
        {
            ct.ThrowIfCancellationRequested();
            var file = Path.GetFileName(d.FilePath);
            var snippet = d.SourceSnippet is { } s ? $"`{s.Replace("`", "'")}`" : "";
            var refsMarkup = d.ReferenceCount == 0 ? "**0**" : d.ReferenceCount.ToString();
            await writer.WriteLineAsync(
                $"| `{d.Name}` | `{d.TypeName}` | {d.Kind} | {file} | {d.Line} | {d.ContainingType} | {d.ContainingMember} | {refsMarkup} | {snippet} |");
        }
    }
}
