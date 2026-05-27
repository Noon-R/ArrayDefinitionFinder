using ArrayFinder.Core.Models;

namespace ArrayFinder.Core.Reporting;

public interface ISymbolReporter
{
    Task WriteAsync(IReadOnlyList<SymbolDeclarationInfo> declarations, TextWriter writer, CancellationToken ct = default);
}
