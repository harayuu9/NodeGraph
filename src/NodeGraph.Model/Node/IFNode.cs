namespace NodeGraph.Model;

/// <summary>
/// 条件分岐ノード。Conditionの値に応じてTrue/Falseの実行フローを分岐します。
/// </summary>
[ExecutionNode("IF", "Control Flow", "True", "False")]
public partial class IFNode
{
    [Input]
    private bool _condition;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        // Conditionの値に応じて、どちらかのExecOutをトリガー
        if (_condition)
        {
            context.TriggerExecOut(0); // True
        }
        else
        {
            context.TriggerExecOut(1); // False
        }

        return Task.CompletedTask;
    }
}
