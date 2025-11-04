namespace NodeGraph.Model;

[Node]
public partial class FloatAddNode
{
    [Input] private float _a = 0f;
    [Input] private float _b = 0f;

    [Output] private float _result;
    public float Result => _result;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        _result = _a + _b;
        return Task.Delay(5000, cancellationToken);
        return Task.CompletedTask;
    }
}
