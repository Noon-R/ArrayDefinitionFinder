using ArrayFinder.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArrayFinder.Core.Analysis;

/// <summary>
/// 指定した DeclarationKind の宣言ノードを収集する SyntaxWalker。
/// 参照数は RefsAnalyzer が SymbolFinder で別途付与する。
/// </summary>
internal sealed class DeclarationWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    private readonly string _filePath;
    private readonly RefsOptions _options;
    private readonly List<(SymbolDeclarationInfo Info, ISymbol Symbol)> _results = [];

    public IReadOnlyList<(SymbolDeclarationInfo Info, ISymbol Symbol)> Results => _results;

    public DeclarationWalker(SemanticModel semanticModel, string filePath, RefsOptions options)
        : base(SyntaxWalkerDepth.Node)
    {
        _semanticModel = semanticModel;
        _filePath = filePath;
        _options = options;
    }

    // ─── フィールド ────────────────────────────────────────────────────────────

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Field))
        {
            foreach (var declarator in node.Declaration.Variables)
            {
                if (_semanticModel.GetDeclaredSymbol(declarator) is IFieldSymbol sym)
                    Add(sym.Name, sym.Type.ToDisplayString(), DeclarationKind.Field, declarator, sym);
            }
        }
        base.VisitFieldDeclaration(node);
    }

    // ─── プロパティ ────────────────────────────────────────────────────────────

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Property))
        {
            if (_semanticModel.GetDeclaredSymbol(node) is IPropertySymbol sym)
                Add(sym.Name, sym.Type.ToDisplayString(), DeclarationKind.Property, node, sym);
        }
        base.VisitPropertyDeclaration(node);
    }

    // ─── メソッド ──────────────────────────────────────────────────────────────

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Method))
        {
            if (_semanticModel.GetDeclaredSymbol(node) is IMethodSymbol sym)
                Add(sym.Name, sym.ReturnType.ToDisplayString(), DeclarationKind.Method, node, sym);
        }
        base.VisitMethodDeclaration(node);
    }

    // ─── コンストラクタ ────────────────────────────────────────────────────────

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Constructor))
        {
            if (_semanticModel.GetDeclaredSymbol(node) is IMethodSymbol sym)
                Add($".ctor({sym.ContainingType.Name})", "", DeclarationKind.Constructor, node, sym);
        }
        base.VisitConstructorDeclaration(node);
    }

    // ─── ローカル変数 ──────────────────────────────────────────────────────────

    public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Local))
        {
            foreach (var declarator in node.Declaration.Variables)
            {
                if (_semanticModel.GetDeclaredSymbol(declarator) is ILocalSymbol sym)
                    Add(sym.Name, sym.Type.ToDisplayString(), DeclarationKind.Local, declarator, sym);
            }
        }
        base.VisitLocalDeclarationStatement(node);
    }

    // ─── パラメータ ────────────────────────────────────────────────────────────

    public override void VisitParameter(ParameterSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Parameter))
        {
            if (_semanticModel.GetDeclaredSymbol(node) is IParameterSymbol sym)
                Add(sym.Name, sym.Type.ToDisplayString(), DeclarationKind.Parameter, node, sym);
        }
        base.VisitParameter(node);
    }

    // ─── 型宣言 ────────────────────────────────────────────────────────────────

    public override void VisitClassDeclaration(ClassDeclarationSyntax node) =>
        VisitTypeDeclaration(node, base.VisitClassDeclaration);

    public override void VisitStructDeclaration(StructDeclarationSyntax node) =>
        VisitTypeDeclaration(node, base.VisitStructDeclaration);

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) =>
        VisitTypeDeclaration(node, base.VisitInterfaceDeclaration);

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Type))
        {
            if (_semanticModel.GetDeclaredSymbol(node) is INamedTypeSymbol sym)
                Add(sym.Name, "", DeclarationKind.Type, node, sym);
        }
        base.VisitEnumDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node) =>
        VisitTypeDeclaration(node, base.VisitRecordDeclaration);

    private void VisitTypeDeclaration<T>(T node, Action<T> baseVisit) where T : TypeDeclarationSyntax
    {
        if (_options.Kinds.Contains(DeclarationKind.Type))
        {
            if (_semanticModel.GetDeclaredSymbol(node) is INamedTypeSymbol sym)
                Add(sym.Name, "", DeclarationKind.Type, node, sym);
        }
        baseVisit(node);
    }

    // ─── イベント ──────────────────────────────────────────────────────────────

    public override void VisitEventDeclaration(EventDeclarationSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Event))
        {
            if (_semanticModel.GetDeclaredSymbol(node) is IEventSymbol sym)
                Add(sym.Name, sym.Type.ToDisplayString(), DeclarationKind.Event, node, sym);
        }
        base.VisitEventDeclaration(node);
    }

    public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        if (_options.Kinds.Contains(DeclarationKind.Event))
        {
            foreach (var declarator in node.Declaration.Variables)
            {
                if (_semanticModel.GetDeclaredSymbol(declarator) is IEventSymbol sym)
                    Add(sym.Name, sym.Type.ToDisplayString(), DeclarationKind.Event, declarator, sym);
            }
        }
        base.VisitEventFieldDeclaration(node);
    }

    // ─── 共通追加メソッド ──────────────────────────────────────────────────────

    private void Add(string name, string typeName, DeclarationKind kind, SyntaxNode node, ISymbol symbol)
    {
        var span = node.GetLocation().GetLineSpan();
        var (containingType, containingMember) = GetContext(symbol);

        _results.Add((new SymbolDeclarationInfo
        {
            Name = name,
            TypeName = typeName,
            Kind = kind,
            FilePath = _filePath,
            Line = span.StartLinePosition.Line + 1,
            Column = span.StartLinePosition.Character + 1,
            ContainingType = containingType,
            ContainingMember = containingMember,
            ReferenceCount = 0,  // RefsAnalyzer が上書き
            SourceSnippet = _options.IncludeSnippets ? TruncateSnippet(node.ToString()) : null,
        }, symbol));
    }

    private static (string containingType, string containingMember) GetContext(ISymbol symbol)
    {
        var containingType = symbol.ContainingType?.Name ?? "";
        var containingMember = "";

        // ローカル/パラメータはどのメソッド/プロパティ内にあるかを示す
        if (symbol is ILocalSymbol or IParameterSymbol)
        {
            var parent = symbol.ContainingSymbol;
            if (parent is IMethodSymbol or IPropertySymbol)
                containingMember = parent.Name;
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
