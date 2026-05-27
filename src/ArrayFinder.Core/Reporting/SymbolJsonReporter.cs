using System.Text.Json;
using System.Text.Json.Serialization;
using ArrayFinder.Core.Models;

namespace ArrayFinder.Core.Reporting;

public sealed class SymbolJsonReporter : ISymbolReporter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task WriteAsync(IReadOnlyList<SymbolDeclarationInfo> declarations, TextWriter writer, CancellationToken ct = default)
    {
        var report = new
        {
            GeneratedAt = DateTimeOffset.Now,
            TotalCount = declarations.Count,
            ZeroRefCount = declarations.Count(d => d.ReferenceCount == 0),
            Summary = declarations
                .GroupBy(d => d.Kind)
                .OrderBy(g => g.Key)
                .Select(g => new { Kind = g.Key, Count = g.Count(), ZeroRefs = g.Count(d => d.ReferenceCount == 0) })
                .ToList(),
            Declarations = declarations,
        };
        var json = JsonSerializer.Serialize(report, s_options);
        await writer.WriteAsync(json.AsMemory(), ct);
    }
}
