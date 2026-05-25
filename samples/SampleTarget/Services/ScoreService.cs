using SampleTarget.Models;

namespace SampleTarget.Services;

public sealed class ScoreService
{
    // TypeDeclaration: フィールド
    private readonly int[] _bonusMultipliers = [2, 3, 5, 10];  // CollectionExpression
    private static readonly string[] s_ranks = new[] { "D", "C", "B", "A", "S" };  // ImplicitCreation

    // TypeDeclaration: 戻り値型
    public int[] GetTopScores(Player[] players, int count)   // TypeDeclaration x2 (引数)
    {
        // MethodReturn: .ToArray() / LINQ
        return players
            .SelectMany(p => p.Scores)
            .OrderByDescending(s => s)
            .Take(count)
            .ToArray();
    }

    // TypeDeclaration: 引数
    public double CalcAverage(int[] scores)
    {
        if (scores.Length == 0) return 0;
        return scores.Average();
    }

    public string GetRank(double average)
    {
        // ArrayCreation: new int[]
        var thresholds = new int[] { 40, 60, 75, 90 };
        for (var i = 0; i < thresholds.Length; i++)
        {
            if (average < thresholds[i])
                return s_ranks[i];
        }
        return s_ranks[^1];
    }

    // 多次元配列 (rank=2)
    public int[,] BuildScoreMatrix(Player[] players)  // TypeDeclaration rank=2
    {
        var matrix = new int[players.Length, 3];      // ArrayCreation rank=2
        for (var i = 0; i < players.Length; i++)
        {
            var scores = players[i].Scores;
            matrix[i, 0] = scores.Length > 0 ? scores.Min() : 0;
            matrix[i, 1] = scores.Length > 0 ? scores.Max() : 0;
            matrix[i, 2] = scores.Length > 0 ? (int)scores.Average() : 0;
        }
        return matrix;
    }
}
