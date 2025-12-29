using NodeGraph.Model;

namespace NodeGraph.SamplePlugin;

/// <summary>
/// サンプルプラグインノード: 文字列を逆順にする
/// </summary>
[Node("Reverse String", "Sample Plugin")]
public partial class SampleReverseStringNode : Node
{
    [Input]
    private string _input = string.Empty;

    [Output]
    private string _output = string.Empty;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var chars = _input.ToCharArray();
        Array.Reverse(chars);
        _output = new string(chars);
        return context.ExecuteOutAsync(0);
    }
}
