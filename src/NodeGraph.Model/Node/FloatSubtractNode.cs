namespace NodeGraph.Model;

[Node]
public partial class FloatSubtractNode
{
    [Input] private float _a;
    [Input] private float _b;

    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = _a - _b;
        await context.ExecuteOutAsync(0);
    }
}
