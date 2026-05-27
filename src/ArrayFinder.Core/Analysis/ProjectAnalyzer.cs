using ArrayFinder.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;
using System.Text.RegularExpressions;

namespace ArrayFinder.Core.Analysis;

public sealed class ProjectAnalyzer
{
    private readonly AnalysisOptions _options;

    public ProjectAnalyzer(AnalysisOptions? options = null)
    {
        _options = options ?? new AnalysisOptions();
    }

    /// <summary>
    /// .sln または .csproj を解析して配列使用箇所の一覧を返す。
    /// MSBuildLocator.RegisterDefaults() は呼び出し元（CLI）で事前に行うこと。
    /// </summary>
    public async Task<AnalysisResult> AnalyzeAsync(
        string projectOrSolutionPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        using var workspace = MSBuildWorkspace.Create();

        workspace.WorkspaceFailed += (_, e) =>
        {
            warnings.Add($"[Workspace] {e.Diagnostic.Kind}: {e.Diagnostic.Message}");
        };

        // (ArrayUsageInfo, 宣言シンボル) の並列リスト
        var resultsWithSymbols = new List<(ArrayUsageInfo Info, ISymbol? Symbol)>();
        var ext = Path.GetExtension(projectOrSolutionPath).ToLowerInvariant();

        if (ext == ".sln")
        {
            progress?.Report($"ソリューションを読み込み中: {projectOrSolutionPath}");
            var solution = await workspace.OpenSolutionAsync(
                projectOrSolutionPath, cancellationToken: cancellationToken);

            foreach (var project in solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AnalyzeProjectAsync(project, resultsWithSymbols, progress, cancellationToken);
            }
        }
        else
        {
            progress?.Report($"プロジェクトを読み込み中: {projectOrSolutionPath}");
            var project = await workspace.OpenProjectAsync(
                projectOrSolutionPath, cancellationToken: cancellationToken);
            await AnalyzeProjectAsync(project, resultsWithSymbols, progress, cancellationToken);
        }

        IReadOnlyList<ArrayUsageInfo> usages;
        if (_options.CountReferences && resultsWithSymbols.Any(r => r.Symbol is not null))
        {
            usages = await EnrichWithReferenceCountsAsync(
                resultsWithSymbols, workspace.CurrentSolution, progress, cancellationToken);
        }
        else
        {
            usages = resultsWithSymbols.Select(r => r.Info).ToList();
        }

        return new AnalysisResult(usages, warnings);
    }

    private async Task AnalyzeProjectAsync(
        Microsoft.CodeAnalysis.Project project,
        List<(ArrayUsageInfo Info, ISymbol? Symbol)> results,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var docs = project.Documents.ToList();
        progress?.Report($"  {project.Name} ({docs.Count} ファイル) 解析中...");

        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation is null) return;

        foreach (var document in docs)
        {
            if (!document.SupportsSyntaxTree) continue;
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = document.FilePath ?? document.Name;
            if (!IsPathIncluded(filePath)) continue;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            if (tree is null) continue;

            var semanticModel = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync(cancellationToken);

            var walker = new ArraySyntaxWalker(semanticModel, filePath, _options);
            walker.Visit(root);

            for (int i = 0; i < walker.Results.Count; i++)
                results.Add((walker.Results[i], walker.Symbols[i]));
        }
    }

    private bool IsPathIncluded(string filePath)
    {
        if (_options.IncludePathPatterns is { Count: > 0 } include)
        {
            if (!include.Any(p => PathMatchesPattern(filePath, p))) return false;
        }
        if (_options.ExcludePathPatterns is { Count: > 0 } exclude)
        {
            if (exclude.Any(p => PathMatchesPattern(filePath, p))) return false;
        }
        return true;
    }

    private static async Task<IReadOnlyList<ArrayUsageInfo>> EnrichWithReferenceCountsAsync(
        List<(ArrayUsageInfo Info, ISymbol? Symbol)> resultsWithSymbols,
        Solution solution,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report("参照数を計上中...");

        // シンボルを重複なく集めてまとめて検索（同一シンボルの重複 API 呼び出しを防ぐ）
        var symbolRefCounts = new Dictionary<ISymbol, int>(SymbolEqualityComparer.Default);
        foreach (var (_, symbol) in resultsWithSymbols)
        {
            if (symbol is null || symbolRefCounts.ContainsKey(symbol)) continue;
            var refs = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken);
            symbolRefCounts[symbol] = refs.Sum(r => r.Locations.Count());
        }

        return resultsWithSymbols
            .Select(r => r.Symbol is { } sym && symbolRefCounts.TryGetValue(sym, out var count)
                ? r.Info with { ReferenceCount = count }
                : r.Info)
            .ToList();
    }

    /// <summary>
    /// ファイルパスとパターンのマッチ判定。
    /// '*' を含まない場合は部分一致（大文字小文字無視）、'*' を含む場合は glob 解釈。
    /// '**' = 任意パスセグメント、'*' = セグメント内任意文字列。
    /// </summary>
    internal static bool PathMatchesPattern(string filePath, string pattern)
    {
        var path = filePath.Replace('\\', '/');
        var pat = pattern.Replace('\\', '/');

        if (!pat.Contains('*'))
            return path.Contains(pat, StringComparison.OrdinalIgnoreCase);

        var sb = new StringBuilder("(?i)");
        int i = 0;
        while (i < pat.Length)
        {
            if (i + 1 < pat.Length && pat[i] == '*' && pat[i + 1] == '*')
            {
                sb.Append(".*");
                i += 2;
                if (i < pat.Length && pat[i] == '/') i++;
            }
            else if (pat[i] == '*')
            {
                sb.Append("[^/]*");
                i++;
            }
            else
            {
                sb.Append(Regex.Escape(pat[i].ToString()));
                i++;
            }
        }
        return Regex.IsMatch(path, sb.ToString());
    }
}

public sealed record AnalysisResult(
    IReadOnlyList<ArrayUsageInfo> Usages,
    IReadOnlyList<string> Warnings);
