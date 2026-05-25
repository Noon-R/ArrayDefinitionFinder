namespace SampleTarget.Models;

public sealed class Player
{
    public string Name { get; init; } = "";
    public int[] Scores { get; set; } = [];           // CollectionExpression → int[]
    public string[] Achievements { get; set; } = [];  // CollectionExpression → string[]
    public int Level { get; set; }
}
