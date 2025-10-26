namespace NodeGraph.Model;

[Node]
public partial class FloatDivideNode
{
    [Input] private float _a = 1f;
    [Input] private float _b = 1f;

    [Output] private float _result;
    public float Result => _result;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        _result = _a / _b;
        return Task.CompletedTask;
    }
}
