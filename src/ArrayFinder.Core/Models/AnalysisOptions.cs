namespace ArrayFinder.Core.Models;

public sealed record AnalysisOptions
{
    public bool IncludeLinqAndMethodReturns { get; init; } = true;
    public bool IncludeSnippets { get; init; } = true;
    public int MaxSnippetLength { get; init; } = 80;

    /// <summary>null = 全型対象。非null = 要素型名（短縮名 or 完全修飾名）でフィルタリング。</summary>
    public IReadOnlyList<string>? FilterElementTypes { get; init; }

    /// <summary>null = 全ファイル対象。非null = いずれかのパターンに一致するファイルのみ処理（部分一致 or glob）。</summary>
    public IReadOnlyList<string>? IncludePathPatterns { get; init; }

    /// <summary>非null = パターンに一致するファイルを除外（部分一致 or glob）。</summary>
    public IReadOnlyList<string>? ExcludePathPatterns { get; init; }

    /// <summary>true のとき TypeDeclaration 宣言シンボルの参照数を SymbolFinder で計上する（処理コスト増）。</summary>
    public bool CountReferences { get; init; } = false;
}
