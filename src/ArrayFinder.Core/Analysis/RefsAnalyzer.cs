using System.Collections.Immutable;
using ArrayFinder.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace ArrayFinder.Core.Analysis;

public sealed class RefsAnalyzer
{
    private readonly RefsOptions _options;

    public RefsAnalyzer(RefsOptions? options = null)
    {
        _options = options ?? new RefsOptions();
    }

    /// <summary>
    /// .sln または .csproj を解析してシンボル宣言の参照数一覧を返す。
    /// MSBuildLocator.RegisterDefaults() は呼び出し元（CLI）で事前に行うこと。
    /// </summary>
    public async Task<RefsAnalysisResult> AnalyzeAsync(
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

        var collected = new List<(SymbolDeclarationInfo Info, ISymbol Symbol)>();
        var ext = Path.GetExtension(projectOrSolutionPath).ToLowerInvariant();

        if (ext == ".sln")
        {
            progress?.Report($"ソリューションを読み込み中: {projectOrSolutionPath}");
            var solution = await workspace.OpenSolutionAsync(
                projectOrSolutionPath, cancellationToken: cancellationToken);

            foreach (var project in solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await CollectFromProjectAsync(project, collected, progress, cancellationToken);
            }
        }
        else
        {
            progress?.Report($"プロジェクトを読み込み中: {projectOrSolutionPath}");
            var project = await workspace.OpenProjectAsync(
                projectOrSolutionPath, cancellationToken: cancellationToken);
            await CollectFromProjectAsync(project, collected, progress, cancellationToken);
        }

        // FilePath が null のドキュメントを SymbolFinder から除外する
        // （source generator 等の生成ファイルで Path.Combine(null, ...) が発生するため）
        var searchDocuments = workspace.CurrentSolution.Projects
            .SelectMany(p => p.Documents)
            .Where(d => d.FilePath is not null)
            .ToImmutableHashSet();

        var declarations = await CountReferencesAsync(
            collected, workspace.CurrentSolution, searchDocuments, progress, cancellationToken);

        return new RefsAnalysisResult(declarations, warnings);
    }

    private async Task CollectFromProjectAsync(
        Project project,
        List<(SymbolDeclarationInfo Info, ISymbol Symbol)> results,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var docs = project.Documents.ToList();
        progress?.Report($"  {project.Name} ({docs.Count} ファイル) 走査中...");

        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation is null) return;

        foreach (var document in docs)
        {
            if (!document.SupportsSyntaxTree) continue;
            cancellationToken.ThrowIfCancellationRequested();

            // FilePath が null のドキュメント（生成ファイル等）は収集対象から除外
            var filePath = document.FilePath;
            if (string.IsNullOrEmpty(filePath)) continue;

            if (!IsPathIncluded(filePath)) continue;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            if (tree is null) continue;

            var semanticModel = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync(cancellationToken);

            var walker = new DeclarationWalker(semanticModel, filePath, _options);
            walker.Visit(root);
            results.AddRange(walker.Results);
        }
    }

    private static async Task<IReadOnlyList<SymbolDeclarationInfo>> CountReferencesAsync(
        List<(SymbolDeclarationInfo Info, ISymbol Symbol)> items,
        Solution solution,
        ImmutableHashSet<Document> searchDocuments,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report($"参照数を計上中... ({items.Count} シンボル)");

        // 同一シンボルの重複 API 呼び出しを排除
        var refCounts = new Dictionary<ISymbol, int>(SymbolEqualityComparer.Default);
        int done = 0;
        foreach (var (_, symbol) in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (refCounts.ContainsKey(symbol)) { done++; continue; }

            // FilePath が null のドキュメントを除外した集合で検索
            var refs = await SymbolFinder.FindReferencesAsync(
                symbol, solution, searchDocuments, cancellationToken);
            refCounts[symbol] = refs.Sum(r => r.Locations.Count());

            done++;
            if (done % 50 == 0)
                progress?.Report($"参照数を計上中... ({done}/{items.Count})");
        }

        return items
            .Select(r => r.Info with { ReferenceCount = refCounts.TryGetValue(r.Symbol, out var c) ? c : 0 })
            .ToList();
    }

    private bool IsPathIncluded(string filePath)
    {
        if (_options.IncludePathPatterns is { Count: > 0 } include)
        {
            if (!include.Any(p => ProjectAnalyzer.PathMatchesPattern(filePath, p))) return false;
        }
        if (_options.ExcludePathPatterns is { Count: > 0 } exclude)
        {
            if (exclude.Any(p => ProjectAnalyzer.PathMatchesPattern(filePath, p))) return false;
        }
        return true;
    }
}

public sealed record RefsAnalysisResult(
    IReadOnlyList<SymbolDeclarationInfo> Declarations,
    IReadOnlyList<string> Warnings);
