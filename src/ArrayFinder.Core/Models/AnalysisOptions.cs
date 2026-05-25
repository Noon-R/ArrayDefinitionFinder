namespace ArrayFinder.Core.Models;

public sealed record AnalysisOptions
{
    public bool IncludeLinqAndMethodReturns { get; init; } = true;
    public bool IncludeSnippets { get; init; } = true;
    public int MaxSnippetLength { get; init; } = 80;

    /// <summary>
    /// null = 全型対象。非null = 要素型名（短縮名 or 完全修飾名）でフィルタリング。
    /// </summary>
    public IReadOnlyList<string>? FilterElementTypes { get; init; }
}
