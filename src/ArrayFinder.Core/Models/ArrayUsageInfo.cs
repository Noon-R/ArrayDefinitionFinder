namespace ArrayFinder.Core.Models;

public enum ArrayKind
{
    TypeDeclaration,      // int[] x, string[] args, MyType[] field
    ArrayCreation,        // new int[] { }, new string[3]
    ImplicitCreation,     // new[] { 1, 2, 3 }
    CollectionExpression, // [1, 2, 3]  (C# 12)
    MethodReturn,         // .ToArray(), Array.Empty<T>(), etc.
}

public sealed record ArrayUsageInfo
{
    public required string ElementType { get; init; }
    public required ArrayKind Kind { get; init; }
    public required string FilePath { get; init; }
    public required int Line { get; init; }
    public required int Column { get; init; }
    public required string ContainingType { get; init; }
    public required string ContainingMember { get; init; }
    public int Rank { get; init; } = 1;
    public string? SourceSnippet { get; init; }
    public string? MethodName { get; init; }
    /// <summary>--count-refs / --zero-refs 有効時のみ設定。null = 未計上。</summary>
    public int? ReferenceCount { get; init; }
}
