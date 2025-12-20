namespace NodeGraph.Model;

[Node(HasExecIn = false, HasExecOut = false)]
public partial class FloatAddNode
{
    [Input] private float _a = 0f;
    [Input] private float _b = 0f;

    [Output] private float _result;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = _a + _b;
        return Task.CompletedTask;
    }
}
