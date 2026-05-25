using ArrayFinder.Core.Models;
using Microsoft.CodeAnalysis.MSBuild;

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
            var msg = $"[Workspace] {e.Diagnostic.Kind}: {e.Diagnostic.Message}";
            warnings.Add(msg);
        };

        var usages = new List<ArrayUsageInfo>();
        var ext = Path.GetExtension(projectOrSolutionPath).ToLowerInvariant();

        if (ext == ".sln")
        {
            progress?.Report($"ソリューションを読み込み中: {projectOrSolutionPath}");
            var solution = await workspace.OpenSolutionAsync(
                projectOrSolutionPath, cancellationToken: cancellationToken);

            foreach (var project in solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AnalyzeProjectAsync(project, usages, progress, cancellationToken);
            }
        }
        else
        {
            progress?.Report($"プロジェクトを読み込み中: {projectOrSolutionPath}");
            var project = await workspace.OpenProjectAsync(
                projectOrSolutionPath, cancellationToken: cancellationToken);
            await AnalyzeProjectAsync(project, usages, progress, cancellationToken);
        }

        return new AnalysisResult(usages, warnings);
    }

    private async Task AnalyzeProjectAsync(
        Microsoft.CodeAnalysis.Project project,
        List<ArrayUsageInfo> results,
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

            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            if (tree is null) continue;

            var semanticModel = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync(cancellationToken);
            var filePath = document.FilePath ?? document.Name;

            var walker = new ArraySyntaxWalker(semanticModel, filePath, _options);
            walker.Visit(root);
            results.AddRange(walker.Results);
        }
    }
}

public sealed record AnalysisResult(
    IReadOnlyList<ArrayUsageInfo> Usages,
    IReadOnlyList<string> Warnings);
