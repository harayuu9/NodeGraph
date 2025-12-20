namespace NodeGraph.Model;

/// <summary>
/// 実行フローのエントリーポイントノード。開始点として使用します。
/// </summary>
[Node("Start", "Control Flow", "Out", HasExecIn = false)]
public partial class StartNode
{
    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        // 最初のノードを実行
        await context.ExecuteOutAsync(0);
    }
}
