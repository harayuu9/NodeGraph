namespace NodeGraph.Model;

/// <summary>
/// 指定回数ループするノード。
/// </summary>
[ExecutionNode("Loop", "Control Flow", "LoopBody", "Completed")]
public partial class LoopNode
{
    [Property(DisplayName = "Count", Tooltip = "ループ回数")]
    [Range(0, 100)]
    private int _count = 3; // ループ回数（プロパティとして設定）

    [Output]
    private int _index; // 現在のループインデックス

    private int _currentIndex = 0;

    public void SetCount(int count)
    {
        _count = count;
    }

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"LoopNode: _currentIndex={_currentIndex}, _count={_count}");
        if (_currentIndex < _count)
        {
            _index = _currentIndex;
            _currentIndex++;
            Console.WriteLine($"LoopNode: Triggering LoopBody (ExecOut[0]), next index will be {_currentIndex}");
            TriggerExecOut(0); // LoopBodyを実行
        }
        
        else
        {
            Console.WriteLine($"LoopNode: Triggering Completed (ExecOut[1])");
            _currentIndex = 0; // リセット
            TriggerExecOut(1); // Completedを実行
        }

        return Task.CompletedTask;
    }
}
