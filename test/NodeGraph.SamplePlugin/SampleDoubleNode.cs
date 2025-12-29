using NodeGraph.Model;

namespace NodeGraph.SamplePlugin;

/// <summary>
/// サンプルプラグインノード: 入力値を2倍にする
/// </summary>
[Node("Double", "Sample Plugin")]
public partial class SampleDoubleNode : Node
{
    [Input]
    private float _input;

    [Output]
    private float _output;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _output = _input * 2;
        return context.ExecuteOutAsync(0);
    }
}
