namespace ArrayFinder.Core.Models;

public sealed record RefsOptions
{
    /// <summary>収集対象の宣言種別。null = 全種別。</summary>
    public IReadOnlySet<DeclarationKind> Kinds { get; init; } =
        new HashSet<DeclarationKind> { DeclarationKind.Field, DeclarationKind.Property };

    public IReadOnlyList<string>? IncludePathPatterns { get; init; }
    public IReadOnlyList<string>? ExcludePathPatterns { get; init; }

    public bool IncludeSnippets { get; init; } = true;
    public int MaxSnippetLength { get; init; } = 80;
}
