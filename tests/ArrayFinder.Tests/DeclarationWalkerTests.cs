using ArrayFinder.Core.Analysis;
using ArrayFinder.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ArrayFinder.Tests;

public class DeclarationWalkerTests
{
    private static IReadOnlyList<SymbolDeclarationInfo> Analyze(string code, RefsOptions? options = null)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
             MetadataReference.CreateFromFile(typeof(Action).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(tree);
        var walker = new DeclarationWalker(semanticModel, "test.cs", options ?? new RefsOptions
        {
            Kinds = new HashSet<DeclarationKind>(Enum.GetValues<DeclarationKind>()),
        });
        walker.Visit(tree.GetRoot());
        // ReferenceCount は 0 のまま（SymbolFinder 不要で単体テスト可能）
        return walker.Results.Select(r => r.Info).ToList();
    }

    [Fact]
    public void Field_Detected()
    {
        var results = Analyze("class C { private int _count; }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Field && r.Name == "_count" && r.TypeName == "int");
    }

    [Fact]
    public void Field_MultipleDeclarators()
    {
        var results = Analyze("class C { int _a, _b; }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Field && r.Name == "_a");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Field && r.Name == "_b");
    }

    [Fact]
    public void Property_Detected()
    {
        var results = Analyze("class C { public string Name { get; set; } = \"\"; }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Property && r.Name == "Name");
    }

    [Fact]
    public void Method_Detected()
    {
        var results = Analyze("class C { public void Foo() { } }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Method && r.Name == "Foo");
    }

    [Fact]
    public void Constructor_Detected()
    {
        var results = Analyze("class C { public C() { } }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Constructor);
    }

    [Fact]
    public void Local_Detected()
    {
        var results = Analyze("class C { void M() { int x = 0; } }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Local && r.Name == "x" && r.TypeName == "int");
    }

    [Fact]
    public void Parameter_Detected()
    {
        var results = Analyze("class C { void M(string name) { } }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Parameter && r.Name == "name");
    }

    [Fact]
    public void Type_Detected()
    {
        var results = Analyze("class MyService { }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Type && r.Name == "MyService");
    }

    [Fact]
    public void Event_FieldStyle_Detected()
    {
        var results = Analyze("class C { public event System.Action OnFoo; }");
        Assert.Contains(results, r => r.Kind == DeclarationKind.Event && r.Name == "OnFoo");
    }

    [Fact]
    public void KindFilter_OnlyField_ReturnsOnlyFields()
    {
        var results = Analyze("class C { int _f; public string Name { get; } = \"\"; void M() { } }",
            new RefsOptions { Kinds = new HashSet<DeclarationKind> { DeclarationKind.Field } });
        Assert.All(results, r => Assert.Equal(DeclarationKind.Field, r.Kind));
        Assert.Single(results);
    }

    [Fact]
    public void ContainingType_IsSet()
    {
        var results = Analyze("class MyClass { int _f; }",
            new RefsOptions { Kinds = new HashSet<DeclarationKind> { DeclarationKind.Field } });
        Assert.All(results, r => Assert.Equal("MyClass", r.ContainingType));
    }

    [Fact]
    public void ContainingMember_IsSet_ForLocal()
    {
        var results = Analyze("class C { void MyMethod() { int x = 0; } }",
            new RefsOptions { Kinds = new HashSet<DeclarationKind> { DeclarationKind.Local } });
        var local = Assert.Single(results);
        Assert.Equal("MyMethod", local.ContainingMember);
    }

    [Fact]
    public void FilePath_IsPreserved()
    {
        var tree = CSharpSyntaxTree.ParseText("class C { int _f; }");
        var compilation = CSharpCompilation.Create("T", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var walker = new DeclarationWalker(compilation.GetSemanticModel(tree), "path/to/MyFile.cs",
            new RefsOptions { Kinds = new HashSet<DeclarationKind> { DeclarationKind.Field } });
        walker.Visit(tree.GetRoot());
        Assert.All(walker.Results, r => Assert.Equal("path/to/MyFile.cs", r.Info.FilePath));
    }
}
