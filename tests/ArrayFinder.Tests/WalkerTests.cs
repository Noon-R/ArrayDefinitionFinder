using ArrayFinder.Core.Analysis;
using ArrayFinder.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ArrayFinder.Tests;

public class WalkerTests
{
    private static IReadOnlyList<ArrayUsageInfo> Analyze(string code, AnalysisOptions? options = null)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
             MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
             MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();
        var walker = new ArraySyntaxWalker(semanticModel, "test.cs", options ?? new AnalysisOptions());
        walker.Visit(root);
        return walker.Results;
    }

    [Fact]
    public void TypeDeclaration_LocalVariable_Detected()
    {
        var results = Analyze("class C { void M() { int[] x = null!; } }");
        Assert.Contains(results, r => r.Kind == ArrayKind.TypeDeclaration && r.ElementType == "int");
    }

    [Fact]
    public void TypeDeclaration_Field_Detected()
    {
        var results = Analyze("class C { string[] _field; }");
        Assert.Contains(results, r => r.Kind == ArrayKind.TypeDeclaration && r.ElementType == "string");
    }

    [Fact]
    public void TypeDeclaration_Parameter_Detected()
    {
        var results = Analyze("class C { void M(byte[] data) { } }");
        Assert.Contains(results, r => r.Kind == ArrayKind.TypeDeclaration && r.ElementType == "byte");
    }

    [Fact]
    public void TypeDeclaration_ReturnType_Detected()
    {
        var results = Analyze("class C { int[] GetArr() => null!; }");
        Assert.Contains(results, r => r.Kind == ArrayKind.TypeDeclaration && r.ElementType == "int");
    }

    [Fact]
    public void ArrayCreation_Detected()
    {
        var results = Analyze("class C { void M() { var x = new int[] { 1, 2, 3 }; } }");
        Assert.Contains(results, r => r.Kind == ArrayKind.ArrayCreation && r.ElementType == "int");
    }

    [Fact]
    public void ArrayCreation_NoDoubleCount_WithTypeDecl()
    {
        // int[] x = new int[] {} → TypeDecl(left) + ArrayCreation(right) = 2件
        var results = Analyze("class C { void M() { int[] x = new int[] { }; } }");
        var typeDecls = results.Where(r => r.Kind == ArrayKind.TypeDeclaration).ToList();
        var creations = results.Where(r => r.Kind == ArrayKind.ArrayCreation).ToList();
        Assert.Single(typeDecls);
        Assert.Single(creations);
    }

    [Fact]
    public void ImplicitCreation_Detected()
    {
        var results = Analyze("class C { void M() { var x = new[] { 1, 2, 3 }; } }");
        Assert.Contains(results, r => r.Kind == ArrayKind.ImplicitCreation && r.ElementType == "int");
    }

    [Fact]
    public void MultidimensionalArray_RankIsCorrect()
    {
        var results = Analyze("class C { void M() { int[,] x = null!; } }");
        Assert.Contains(results, r => r.ElementType == "int" && r.Rank == 2);
    }

    [Fact]
    public void MethodReturn_ToArray_Detected()
    {
        var code = """
            using System.Linq;
            using System.Collections.Generic;
            class C {
                void M() {
                    var list = new List<int> { 1, 2 };
                    var arr = list.ToArray();
                }
            }
            """;
        var results = Analyze(code);
        Assert.Contains(results, r => r.Kind == ArrayKind.MethodReturn
                                   && r.ElementType == "int"
                                   && r.MethodName == "ToArray");
    }

    [Fact]
    public void MethodReturn_Excluded_WhenNoLinqOption()
    {
        var code = """
            using System.Linq;
            using System.Collections.Generic;
            class C {
                void M() {
                    var list = new List<int>();
                    var arr = list.ToArray();
                }
            }
            """;
        var options = new AnalysisOptions { IncludeLinqAndMethodReturns = false };
        var results = Analyze(code, options);
        Assert.DoesNotContain(results, r => r.Kind == ArrayKind.MethodReturn);
    }

    [Fact]
    public void FilterByElementType_Works()
    {
        var code = """
            class C {
                void M() {
                    int[] a = new int[1];
                    string[] b = new string[1];
                }
            }
            """;
        var options = new AnalysisOptions
        {
            FilterElementTypes = ["int"],
            IncludeLinqAndMethodReturns = false,
        };
        var results = Analyze(code, options);
        Assert.All(results, r => Assert.Equal("int", r.ElementType));
    }

    [Fact]
    public void ContainingContext_IsCorrect()
    {
        var results = Analyze("class MyClass { void MyMethod() { int[] x = null!; } }");
        var r = Assert.Single(results, r => r.Kind == ArrayKind.TypeDeclaration);
        Assert.Equal("MyClass", r.ContainingType);
        Assert.Equal("MyMethod", r.ContainingMember);
    }

    // ─── ジェネリック型パラメータのスキップ ──────────────────────────────────

    [Fact]
    public void Generic_NakedTypeParam_Skipped()
    {
        // T[] はスキップ（定義サイドの T）
        var code = """
            class Repository<T>
            {
                private T[] _items = System.Array.Empty<T>();
                public T[] GetAll() => _items;
                public void Process(T[] items) { }
            }
            """;
        var results = Analyze(code);
        Assert.DoesNotContain(results, r => r.ElementType == "T");
    }

    [Fact]
    public void Generic_TypeParamInTypeArg_Skipped()
    {
        // List<T>[] のような「型引数に T を含む」ケースもスキップ
        var code = """
            using System.Collections.Generic;
            class C<T>
            {
                List<T>[] _groups = new List<T>[3];
            }
            """;
        var results = Analyze(code);
        Assert.DoesNotContain(results, r => r.ElementType.Contains('T'));
    }

    [Fact]
    public void Generic_CallSite_ConcreteType_Detected()
    {
        // 呼び出し側では具体型が解決されるので検知されるべき
        var code = """
            using System.Linq;
            using System.Collections.Generic;
            class C {
                void M() {
                    var list = new List<int>();
                    int[] arr = list.ToArray();  // MethodReturn: int
                }
            }
            """;
        var results = Analyze(code);
        Assert.Contains(results, r => r.Kind == ArrayKind.MethodReturn && r.ElementType == "int");
        Assert.DoesNotContain(results, r => r.ElementType == "T");
    }

    [Fact]
    public void Generic_MultipleTypeParams_AllSkipped()
    {
        var code = """
            class C<TKey, TValue>
            {
                TKey[] _keys = new TKey[0];
                TValue[] _vals = System.Array.Empty<TValue>();
                public TKey[] GetKeys() => _keys;
            }
            """;
        var results = Analyze(code);
        Assert.DoesNotContain(results, r => r.ElementType is "TKey" or "TValue");
    }
}
