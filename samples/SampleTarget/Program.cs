using SampleTarget.Models;
using SampleTarget.Services;

var players = new Player[]   // ArrayCreation → Player[]
{
    new() { Name = "Alice", Level = 3, Scores = [95, 87, 72, 100] },
    new() { Name = "Bob",   Level = 2, Scores = [60, 55, 70]      },
    new() { Name = "Carol", Level = 5, Scores = [30, 42, 38]      },
};

var scoreService = new ScoreService();
var report = new ReportGenerator();

// MethodReturn: ToArray
var top3 = scoreService.GetTopScores(players, 3);
Console.WriteLine($"Top 3: {string.Join(", ", top3)}");

// int[,]
var matrix = scoreService.BuildScoreMatrix(players);
Console.WriteLine($"Matrix[0,1] (Alice max): {matrix[0, 1]}");

// byte[]
var bytes = report.SerializeScores(top3);
Console.WriteLine($"Serialized bytes: {bytes.Length}");

// string[]
var headers = report.GetColumnHeaders();
Console.WriteLine($"Headers: {string.Join(" | ", headers)}");
