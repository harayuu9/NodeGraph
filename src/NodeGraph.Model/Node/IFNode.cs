namespace NodeGraph.Model;

/// <summary>
/// 条件分岐ノード。Conditionの値に応じてTrue/Falseの実行フローを分岐します。
/// </summary>
[Node("IF", "Control Flow", "True", "False")]
public partial class IFNode
{
    [Input]
    private bool _condition;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        // Conditionの値に応じて、どちらかのExecOutを実行
        if (_condition)
        {
            await context.ExecuteOutAsync(0); // True
        }
        else
        {
            await context.ExecuteOutAsync(1); // False
        }
    }
}
