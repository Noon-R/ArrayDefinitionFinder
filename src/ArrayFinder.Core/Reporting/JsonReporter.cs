using System.Text.Json;
using System.Text.Json.Serialization;
using ArrayFinder.Core.Models;

namespace ArrayFinder.Core.Reporting;

public sealed class JsonReporter : IReporter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task WriteAsync(IReadOnlyList<ArrayUsageInfo> usages, TextWriter writer, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(BuildReport(usages), s_options);
        await writer.WriteAsync(json.AsMemory(), ct);
    }

    private static object BuildReport(IReadOnlyList<ArrayUsageInfo> usages) => new
    {
        GeneratedAt = DateTimeOffset.Now,
        TotalCount = usages.Count,
        Summary = usages
            .GroupBy(u => u.ElementType)
            .OrderByDescending(g => g.Count())
            .Select(g => new { ElementType = g.Key, Count = g.Count() })
            .ToList(),
        Usages = usages,
    };
}
