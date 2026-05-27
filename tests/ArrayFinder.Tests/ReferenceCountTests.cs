using ArrayFinder.Core.Analysis;
using ArrayFinder.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ArrayFinder.Tests;

public class ReferenceCountTests
{
    private static IReadOnlyList<(ArrayUsageInfo Result, ISymbol? Symbol)>
        Analyze(string code, AnalysisOptions? options = null)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(tree);
        var walker = new ArraySyntaxWalker(semanticModel, "test.cs", options ?? new AnalysisOptions());
        walker.Visit(tree.GetRoot());
        return walker.Results.Zip(walker.Symbols, (r, s) => (r, s)).ToList();
    }

    [Fact]
    public void CountReferences_False_AllSymbolsNull()
    {
        var pairs = Analyze(
            "class C { int[] _f; void M(string[] p) { } }",
            new AnalysisOptions { CountReferences = false });

        Assert.NotEmpty(pairs);
        Assert.All(pairs, p => Assert.Null(p.Symbol));
    }

    [Fact]
    public void CountReferences_True_FieldSymbol_IsNotNull()
    {
        var pairs = Analyze(
            "class C { int[] _field; }",
            new AnalysisOptions { CountReferences = true });

        var (_, symbol) = pairs.First(p => p.Result.Kind == ArrayKind.TypeDeclaration);
        Assert.NotNull(symbol);
        Assert.IsAssignableFrom<IFieldSymbol>(symbol);
    }

    [Fact]
    public void CountReferences_True_LocalSymbol_IsNotNull()
    {
        var pairs = Analyze(
            "class C { void M() { int[] x = null!; } }",
            new AnalysisOptions { CountReferences = true });

        var (_, symbol) = pairs.First(p => p.Result.Kind == ArrayKind.TypeDeclaration);
        Assert.NotNull(symbol);
        Assert.IsAssignableFrom<ILocalSymbol>(symbol);
    }

    [Fact]
    public void CountReferences_True_ParameterSymbol_IsNotNull()
    {
        var pairs = Analyze(
            "class C { void M(byte[] data) { } }",
            new AnalysisOptions { CountReferences = true });

        var (_, symbol) = pairs.First(p => p.Result.Kind == ArrayKind.TypeDeclaration);
        Assert.NotNull(symbol);
        Assert.IsAssignableFrom<IParameterSymbol>(symbol);
    }

    [Fact]
    public void CountReferences_True_MethodReturnType_SymbolIsNull()
    {
        // メソッド戻り値の ArrayType はメソッドシンボル ≠ 配列シンボルなので null
        var pairs = Analyze(
            "class C { int[] GetArr() => null!; }",
            new AnalysisOptions { CountReferences = true });

        var (_, symbol) = pairs.First(p => p.Result.Kind == ArrayKind.TypeDeclaration);
        Assert.Null(symbol);
    }

    [Fact]
    public void ResultsAndSymbols_AlwaysParallel()
    {
        var pairs = Analyze(
            "class C { int[] f; void M(string[] p) { byte[] x = null!; } }",
            new AnalysisOptions { CountReferences = true });

        // zip が成立 ＝ 長さが一致している
        Assert.Equal(3, pairs.Count);
    }
}
