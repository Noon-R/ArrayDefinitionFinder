# ArrayDefinitionFinder

C# プロジェクト内で **配列が使われている型と箇所を一覧化する** Roslyn ベースのスタンドアロン CLI ツールです。

## 検出できる構文

| Kind | 例 |
|------|----|
| `TypeDeclaration` | `int[] x`, フィールド, 引数, 戻り値型 |
| `ArrayCreation` | `new int[] {}`, `new string[3]` |
| `ImplicitCreation` | `new[] { 1, 2, 3 }` |
| `CollectionExpression` | `[1, 2, 3]` (C# 12) |
| `MethodReturn` | `.ToArray()`, `Array.Empty<T>()` 等 |

多次元配列 (`int[,]`) のランク検出、ジャグ配列 (`int[][]`) にも対応しています。

## 使い方

```bash
# ソリューション全体を解析（コンソール出力）
dotnet run --project src/ArrayFinder.Cli -- MyApp.sln

# JSON レポート出力
dotnet run --project src/ArrayFinder.Cli -- MyApp.sln --format json --output report.json

# CSV 出力
dotnet run --project src/ArrayFinder.Cli -- MyApp.sln --format csv --output report.csv

# Markdown テーブル出力
dotnet run --project src/ArrayFinder.Cli -- MyApp.sln --format markdown --output report.md

# 特定の要素型だけに絞る
dotnet run --project src/ArrayFinder.Cli -- MyApp.sln --filter-type int,string

# LINQ・メソッド戻り値による生成を除外
dotnet run --project src/ArrayFinder.Cli -- MyApp.sln --no-linq

# 要素型でソート
dotnet run --project src/ArrayFinder.Cli -- MyApp.sln --sort type
```

## オプション一覧

| オプション | 既定値 | 説明 |
|-----------|--------|------|
| `[PATH]` | カレントディレクトリを検索 | `.sln` または `.csproj` |
| `-f, --format` | `console` | `console` / `json` / `csv` / `markdown` |
| `-o, --output` | 標準出力 | 出力ファイルパス |
| `--no-linq` | false | MethodReturn 種別を除外 |
| `--no-snippet` | false | ソースコードスニペットを非表示 |
| `--filter-type` | (全型) | 要素型でフィルタリング（カンマ区切り） |
| `--sort` | `file` | `file` / `type` / `kind` |

## プロジェクト構成

```
src/
  ArrayFinder.Core/       ← Roslyn 解析ライブラリ
    Analysis/
      ArraySyntaxWalker.cs   CSharpSyntaxWalker 実装
      ProjectAnalyzer.cs     MSBuildWorkspace でプロジェクト読み込み
    Models/
      ArrayUsageInfo.cs      検出結果レコード
      AnalysisOptions.cs     解析オプション
    Reporting/
      JsonReporter.cs / CsvReporter.cs / MarkdownReporter.cs
  ArrayFinder.Cli/        ← コンソールアプリ (Spectre.Console.Cli)
    Commands/AnalyzeCommand.cs
    Reporting/ConsoleReporter.cs
samples/
  SampleTarget/           ← 動作確認用サンプルプロジェクト
tests/
  ArrayFinder.Tests/      ← xUnit テスト
```

## 動作要件

- .NET 8 SDK
- MSBuild（.NET SDK に同梱）
