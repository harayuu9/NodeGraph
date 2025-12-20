namespace NodeGraph.Model;

/// <summary>
/// 指定回数ループするノード。
/// ノード内でループを完結し、外部ループバック接続は不要です。
/// </summary>
[Node("Loop", "Control Flow", "LoopBody", "Completed")]
public partial class LoopNode
{
    [Property(DisplayName = "Count", Tooltip = "ループ回数")]
    [Range(0, 100)]
    private int _count = 3;

    [Output]
    private int _index;

    public void SetCount(int count)
    {
        _count = count;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        for (int i = 0; i < _count; i++)
        {
            _index = i;
            await context.ExecuteOutAsync(0); // LoopBody
        }
        await context.ExecuteOutAsync(1); // Completed
    }
}
