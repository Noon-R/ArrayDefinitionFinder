namespace ArrayFinder.Core.Models;

public sealed record SymbolDeclarationInfo
{
    public required string Name { get; init; }
    /// <summary>フィールド/プロパティ/ローカルの宣言型。メソッド/型は空文字。</summary>
    public required string TypeName { get; init; }
    public required DeclarationKind Kind { get; init; }
    public required string FilePath { get; init; }
    public required int Line { get; init; }
    public required int Column { get; init; }
    public required string ContainingType { get; init; }
    public required string ContainingMember { get; init; }
    public required int ReferenceCount { get; init; }
    public string? SourceSnippet { get; init; }
}
