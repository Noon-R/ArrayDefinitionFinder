using SampleTarget.Models;

namespace SampleTarget.Services;

public sealed class ReportGenerator
{
    // ジャグ配列 (配列の配列)
    public int[][] GroupScoresByLevel(Player[] players)  // TypeDeclaration (戻り値 int[][])
    {
        var groups = new int[5][];                       // ArrayCreation → int[][]
        for (var level = 1; level <= 5; level++)
        {
            var levelScores = players
                .Where(p => p.Level == level)
                .SelectMany(p => p.Scores)
                .ToArray();                              // MethodReturn: ToArray
            groups[level - 1] = levelScores;
        }
        return groups;
    }

    // byte[] を扱う I/O 系
    public byte[] SerializeScores(int[] scores)          // TypeDeclaration x2
    {
        // Array.Empty<T>() によるショートカット
        if (scores.Length == 0)
            return Array.Empty<byte>();                  // MethodReturn: Array.Empty

        var buffer = new byte[scores.Length * 4];        // ArrayCreation: byte[]
        for (var i = 0; i < scores.Length; i++)
            BitConverter.TryWriteBytes(buffer.AsSpan(i * 4), scores[i]);

        return buffer;
    }

    // C# 12 コレクション式
    public string[] GetColumnHeaders()
    {
        return ["Player", "Min", "Max", "Avg", "Rank"]; // CollectionExpression → string[]
    }

    // 暗黙型配列
    public void PrintSummary(Player player)
    {
        var labels = new[] { "Name", "Level", "Score Count" };  // ImplicitCreation → string[]
        var values = new[] { player.Name, player.Level.ToString(), player.Scores.Length.ToString() };

        for (var i = 0; i < labels.Length; i++)
            Console.WriteLine($"{labels[i]}: {values[i]}");
    }
}
