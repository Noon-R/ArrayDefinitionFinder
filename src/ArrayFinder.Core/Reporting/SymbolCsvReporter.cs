using ArrayFinder.Core.Models;

namespace ArrayFinder.Core.Reporting;

public sealed class SymbolCsvReporter : ISymbolReporter
{
    public async Task WriteAsync(IReadOnlyList<SymbolDeclarationInfo> declarations, TextWriter writer, CancellationToken ct = default)
    {
        await writer.WriteLineAsync("Name,TypeName,Kind,FilePath,Line,Column,ContainingType,ContainingMember,ReferenceCount,Snippet");

        foreach (var d in declarations)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(
                $"{Escape(d.Name)}," +
                $"{Escape(d.TypeName)}," +
                $"{d.Kind}," +
                $"{Escape(d.FilePath)}," +
                $"{d.Line}," +
                $"{d.Column}," +
                $"{Escape(d.ContainingType)}," +
                $"{Escape(d.ContainingMember)}," +
                $"{d.ReferenceCount}," +
                $"{Escape(d.SourceSnippet ?? "")}");
        }
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
