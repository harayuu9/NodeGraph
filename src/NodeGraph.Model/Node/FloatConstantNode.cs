namespace NodeGraph.Model;

[Node]
public partial class FloatConstantNode
{
    [Output] private float _value;

    public void SetValue(float value)
    {
        _value = value;
    }

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
