using ArrayFinder.Core.Models;

namespace ArrayFinder.Core.Reporting;

public interface IReporter
{
    Task WriteAsync(IReadOnlyList<ArrayUsageInfo> usages, TextWriter writer, CancellationToken ct = default);
}
