using ArrayFinder.Core.Analysis;

namespace ArrayFinder.Tests;

public class PathPatternTests
{
    // ─── 部分一致（ワイルドカードなし） ──────────────────────────────────────

    [Theory]
    [InlineData(@"D:\src\MyModule\Foo.cs", "MyModule", true)]
    [InlineData(@"D:\src\MyModule\Foo.cs", "MYMODULE", true)]   // 大文字小文字無視
    [InlineData(@"D:\src\MyModule\Foo.cs", "OtherModule", false)]
    [InlineData(@"D:\src\MyModule\Foo.cs", "src/MyModule", true)]  // パス区切り正規化
    public void SubstringMatch(string filePath, string pattern, bool expected)
    {
        Assert.Equal(expected, ProjectAnalyzer.PathMatchesPattern(filePath, pattern));
    }

    // ─── * ワイルドカード（セグメント内） ─────────────────────────────────────

    [Theory]
    [InlineData(@"D:/src/Foo.Designer.cs", "*.Designer.cs", true)]
    [InlineData(@"D:/src/Foo.cs", "*.Designer.cs", false)]
    [InlineData(@"D:/src/subdir/Foo.cs", "*.Designer.cs", false)] // * はセグメントをまたがない
    public void SingleStarGlob(string filePath, string pattern, bool expected)
    {
        Assert.Equal(expected, ProjectAnalyzer.PathMatchesPattern(filePath, pattern));
    }

    // ─── ** ワイルドカード（任意パスセグメント） ───────────────────────────────

    [Theory]
    [InlineData(@"D:/src/tests/UnitTest1.cs", "tests/**", true)]
    [InlineData(@"D:/src/tests/sub/UnitTest2.cs", "tests/**", true)]
    [InlineData(@"D:/src/main/MyClass.cs", "tests/**", false)]
    [InlineData(@"D:/src/any/Foo.Designer.cs", "**/*.Designer.cs", true)]
    [InlineData(@"D:/src/any/Foo.cs", "**/*.Designer.cs", false)]
    public void DoubleStarGlob(string filePath, string pattern, bool expected)
    {
        Assert.Equal(expected, ProjectAnalyzer.PathMatchesPattern(filePath, pattern));
    }
}
