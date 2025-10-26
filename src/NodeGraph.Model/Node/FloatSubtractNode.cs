namespace NodeGraph.Model;

[Node]
public partial class FloatSubtractNode
{
    [Input] private float _a = 0f;
    [Input] private float _b = 0f;

    [Output] private float _result;
    public float Result => _result;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        _result = _a - _b;
        return Task.CompletedTask;
    }
}
