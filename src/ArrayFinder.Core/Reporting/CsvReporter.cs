using ArrayFinder.Core.Models;

namespace ArrayFinder.Core.Reporting;

public sealed class CsvReporter : IReporter
{
    public async Task WriteAsync(IReadOnlyList<ArrayUsageInfo> usages, TextWriter writer, CancellationToken ct = default)
    {
        await writer.WriteLineAsync("ElementType,Kind,Rank,FilePath,Line,Column,ContainingType,ContainingMember,MethodName,Snippet");

        foreach (var u in usages)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(
                $"{Escape(u.ElementType)}," +
                $"{u.Kind}," +
                $"{u.Rank}," +
                $"{Escape(u.FilePath)}," +
                $"{u.Line}," +
                $"{u.Column}," +
                $"{Escape(u.ContainingType)}," +
                $"{Escape(u.ContainingMember)}," +
                $"{Escape(u.MethodName ?? "")}," +
                $"{Escape(u.SourceSnippet ?? "")}");
        }
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
