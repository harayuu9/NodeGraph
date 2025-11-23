namespace NodeGraph.Model;

/// <summary>
/// 実行フローのエントリーポイントノード。データ依存がないExecutionNodeの開始点として使用します。
/// </summary>
[ExecutionNode("Start", "Control Flow", "Out", HasExecIn = false)]
public partial class StartNode
{
    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        // 何もしない。単にExecOutにフローを渡すだけ。
        return Task.CompletedTask;
    }
}
