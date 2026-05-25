# Array Usage Report

Generated: 2026-05-26 01:37:52 +09:00  
Total: **36** usages

## Summary by Element Type

| Element Type | Count |
|---|---|
| `int` | 17 |
| `string` | 9 |
| `SampleTarget.Models.Player` | 4 |
| `byte` | 4 |
| `int[]` | 2 |

## All Usages

| Element Type | Kind | Rank | File | Line | Containing Type | Member | Snippet |
|---|---|---|---|---|---|---|---|
| `int` | TypeDeclaration | 1 | Player.cs | 6 | Player | Scores | `int[]` |
| `int` | CollectionExpression | 1 | Player.cs | 6 | Player | Scores | `[]` |
| `string` | TypeDeclaration | 1 | Player.cs | 7 | Player | Achievements | `string[]` |
| `string` | CollectionExpression | 1 | Player.cs | 7 | Player | Achievements | `[]` |
| `SampleTarget.Models.Player` | ArrayCreation | 1 | Program.cs | 4 |  |  | `new Player[]   // ArrayCreation → Player[] {     new() { Name = "Alice", Leve...` |
| `int` | CollectionExpression | 1 | Program.cs | 6 |  |  | `[95, 87, 72, 100]` |
| `int` | CollectionExpression | 1 | Program.cs | 7 |  |  | `[60, 55, 70]` |
| `int` | CollectionExpression | 1 | Program.cs | 8 |  |  | `[30, 42, 38]` |
| `int` | MethodReturn | 1 | Program.cs | 15 |  |  | `scoreService.GetTopScores(players, 3)` |
| `int` | MethodReturn | 2 | Program.cs | 19 |  |  | `scoreService.BuildScoreMatrix(players)` |
| `byte` | MethodReturn | 1 | Program.cs | 23 |  |  | `report.SerializeScores(top3)` |
| `string` | MethodReturn | 1 | Program.cs | 27 |  |  | `report.GetColumnHeaders()` |
| `int[]` | TypeDeclaration | 1 | ReportGenerator.cs | 8 | ReportGenerator | GroupScoresByLevel | `int[][]` |
| `SampleTarget.Models.Player` | TypeDeclaration | 1 | ReportGenerator.cs | 8 | ReportGenerator | GroupScoresByLevel | `Player[]` |
| `int[]` | ArrayCreation | 1 | ReportGenerator.cs | 10 | ReportGenerator | GroupScoresByLevel | `new int[5][]` |
| `int` | MethodReturn | 1 | ReportGenerator.cs | 13 | ReportGenerator | GroupScoresByLevel | `players                 .Where(p => p.Level == level)                 .Select...` |
| `byte` | TypeDeclaration | 1 | ReportGenerator.cs | 23 | ReportGenerator | SerializeScores | `byte[]` |
| `int` | TypeDeclaration | 1 | ReportGenerator.cs | 23 | ReportGenerator | SerializeScores | `int[]` |
| `byte` | MethodReturn | 1 | ReportGenerator.cs | 27 | ReportGenerator | SerializeScores | `Array.Empty<byte>()` |
| `byte` | ArrayCreation | 1 | ReportGenerator.cs | 29 | ReportGenerator | SerializeScores | `new byte[scores.Length * 4]` |
| `string` | TypeDeclaration | 1 | ReportGenerator.cs | 37 | ReportGenerator | GetColumnHeaders | `string[]` |
| `string` | CollectionExpression | 1 | ReportGenerator.cs | 39 | ReportGenerator | GetColumnHeaders | `["Player", "Min", "Max", "Avg", "Rank"]` |
| `string` | ImplicitCreation | 1 | ReportGenerator.cs | 45 | ReportGenerator | PrintSummary | `new[] { "Name", "Level", "Score Count" }` |
| `string` | ImplicitCreation | 1 | ReportGenerator.cs | 46 | ReportGenerator | PrintSummary | `new[] { player.Name, player.Level.ToString(), player.Scores.Length.ToString() }` |
| `int` | TypeDeclaration | 1 | ScoreService.cs | 8 | ScoreService | _bonusMultipliers | `int[]` |
| `int` | CollectionExpression | 1 | ScoreService.cs | 8 | ScoreService | _bonusMultipliers | `[2, 3, 5, 10]` |
| `string` | TypeDeclaration | 1 | ScoreService.cs | 9 | ScoreService | s_ranks | `string[]` |
| `string` | ImplicitCreation | 1 | ScoreService.cs | 9 | ScoreService | s_ranks | `new[] { "D", "C", "B", "A", "S" }` |
| `int` | TypeDeclaration | 1 | ScoreService.cs | 12 | ScoreService | GetTopScores | `int[]` |
| `SampleTarget.Models.Player` | TypeDeclaration | 1 | ScoreService.cs | 12 | ScoreService | GetTopScores | `Player[]` |
| `int` | MethodReturn | 1 | ScoreService.cs | 15 | ScoreService | GetTopScores | `players             .SelectMany(p => p.Scores)             .OrderByDescending...` |
| `int` | TypeDeclaration | 1 | ScoreService.cs | 23 | ScoreService | CalcAverage | `int[]` |
| `int` | ArrayCreation | 1 | ScoreService.cs | 32 | ScoreService | GetRank | `new int[] { 40, 60, 75, 90 }` |
| `int` | TypeDeclaration | 2 | ScoreService.cs | 42 | ScoreService | BuildScoreMatrix | `int[,]` |
| `SampleTarget.Models.Player` | TypeDeclaration | 1 | ScoreService.cs | 42 | ScoreService | BuildScoreMatrix | `Player[]` |
| `int` | ArrayCreation | 2 | ScoreService.cs | 44 | ScoreService | BuildScoreMatrix | `new int[players.Length, 3]` |
