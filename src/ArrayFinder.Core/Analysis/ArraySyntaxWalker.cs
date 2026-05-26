using ArrayFinder.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArrayFinder.Core.Analysis;

internal sealed class ArraySyntaxWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    private readonly string _filePath;
    private readonly AnalysisOptions _options;
    private readonly List<ArrayUsageInfo> _results = [];

    public IReadOnlyList<ArrayUsageInfo> Results => _results;

    public ArraySyntaxWalker(SemanticModel semanticModel, string filePath, AnalysisOptions options)
        : base(SyntaxWalkerDepth.Node)
    {
        _semanticModel = semanticModel;
        _filePath = filePath;
        _options = options;
    }

    // 型宣言: int[] x, string[] args, MyType[] field, return type など
    // ArrayCreationExpression の直接の子は除外（ArrayCreation として別途捕捉）
    public override void VisitArrayType(ArrayTypeSyntax node)
    {
        if (node.Parent is not ArrayCreationExpressionSyntax)
        {
            if (_semanticModel.GetTypeInfo(node).Type is IArrayTypeSymbol arraySymbol
                && !HasUnresolvedTypeParameter(arraySymbol.ElementType))
            {
                AddIfMatches(arraySymbol.ElementType.ToDisplayString(),
                    ArrayKind.TypeDeclaration, node, rank: arraySymbol.Rank);
            }
        }
        base.VisitArrayType(node);
    }

    // new int[] { } / new int[3]
    public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
    {
        if (_semanticModel.GetTypeInfo(node).Type is IArrayTypeSymbol arraySymbol
            && !HasUnresolvedTypeParameter(arraySymbol.ElementType))
        {
            AddIfMatches(arraySymbol.ElementType.ToDisplayString(),
                ArrayKind.ArrayCreation, node, rank: arraySymbol.Rank);
        }
        base.VisitArrayCreationExpression(node);
    }

    // new[] { 1, 2, 3 }
    public override void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
    {
        if (_semanticModel.GetTypeInfo(node).Type is IArrayTypeSymbol arraySymbol
            && !HasUnresolvedTypeParameter(arraySymbol.ElementType))
        {
            AddIfMatches(arraySymbol.ElementType.ToDisplayString(),
                ArrayKind.ImplicitCreation, node, rank: arraySymbol.Rank);
        }
        base.VisitImplicitArrayCreationExpression(node);
    }

    // [1, 2, 3]  (C# 12 コレクション式 → 配列ターゲットのみ対象)
    public override void VisitCollectionExpression(CollectionExpressionSyntax node)
    {
        var typeInfo = _semanticModel.GetTypeInfo(node);
        var targetType = typeInfo.ConvertedType ?? typeInfo.Type;
        if (targetType is IArrayTypeSymbol arraySymbol
            && !HasUnresolvedTypeParameter(arraySymbol.ElementType))
        {
            AddIfMatches(arraySymbol.ElementType.ToDisplayString(),
                ArrayKind.CollectionExpression, node, rank: arraySymbol.Rank);
        }
        base.VisitCollectionExpression(node);
    }

    // .ToArray(), Array.Empty<T>(), Enumerable.Repeat<T>() など
    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (_options.IncludeLinqAndMethodReturns
            && _semanticModel.GetTypeInfo(node).Type is IArrayTypeSymbol arraySymbol
            && !HasUnresolvedTypeParameter(arraySymbol.ElementType))
        {
            var methodName = node.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax i => i.Identifier.Text,
                _ => node.Expression.ToString(),
            };

            AddIfMatches(arraySymbol.ElementType.ToDisplayString(),
                ArrayKind.MethodReturn, node, rank: arraySymbol.Rank, methodName: methodName);
        }
        base.VisitInvocationExpression(node);
    }

    /// T, TResult, List&lt;T&gt; など未解決型パラメータを含む場合 true
    private static bool HasUnresolvedTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        IArrayTypeSymbol arr  => HasUnresolvedTypeParameter(arr.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(HasUnresolvedTypeParameter),
        _ => false,
    };

    private void AddIfMatches(
        string elementType,
        ArrayKind kind,
        SyntaxNode node,
        int rank = 1,
        string? methodName = null)
    {
        if (_options.FilterElementTypes is { Count: > 0 } filter)
        {
            var shortName = elementType.Contains('.')
                ? elementType[(elementType.LastIndexOf('.') + 1)..]
                : elementType;
            if (!filter.Any(f => f.Equals(elementType, StringComparison.OrdinalIgnoreCase)
                              || f.Equals(shortName, StringComparison.OrdinalIgnoreCase)))
                return;
        }

        var span = node.GetLocation().GetLineSpan();
        var (containingType, containingMember) = GetContainingContext(node);

        _results.Add(new ArrayUsageInfo
        {
            ElementType = elementType,
            Kind = kind,
            FilePath = _filePath,
            Line = span.StartLinePosition.Line + 1,
            Column = span.StartLinePosition.Character + 1,
            ContainingType = containingType,
            ContainingMember = containingMember,
            Rank = rank,
            SourceSnippet = _options.IncludeSnippets ? TruncateSnippet(node.ToString()) : null,
            MethodName = methodName,
        });
    }

    private static (string containingType, string containingMember) GetContainingContext(SyntaxNode node)
    {
        string containingType = "";
        string containingMember = "";
        var current = node.Parent;

        while (current is not null)
        {
            if (containingMember == "")
            {
                containingMember = current switch
                {
                    MethodDeclarationSyntax m => m.Identifier.Text,
                    ConstructorDeclarationSyntax c => $".ctor({c.Identifier.Text})",
                    PropertyDeclarationSyntax p => p.Identifier.Text,
                    FieldDeclarationSyntax f => f.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "field",
                    LocalFunctionStatementSyntax lf => lf.Identifier.Text,
                    AccessorDeclarationSyntax a => a.Keyword.Text,
                    _ => "",
                };
            }

            if (current is TypeDeclarationSyntax typeDecl)
            {
                containingType = typeDecl.Identifier.Text;
                break;
            }

            current = current.Parent;
        }

        return (containingType, containingMember);
    }

    private string TruncateSnippet(string text)
    {
        var single = text.Replace("\r\n", " ").Replace('\n', ' ').Trim();
        return single.Length <= _options.MaxSnippetLength
            ? single
            : single[..(_options.MaxSnippetLength - 3)] + "...";
    }
}
