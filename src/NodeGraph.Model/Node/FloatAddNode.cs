namespace NodeGraph.Model;

[Node(HasExecIn = true, HasExecOut = true)]
public partial class FloatAddNode
{
    [Input] private float _a = 0f;
    [Input] private float _b = 0f;

    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = _a + _b;
        await Task.Delay(500);
        await context.ExecuteOutAsync(0);
    }
}