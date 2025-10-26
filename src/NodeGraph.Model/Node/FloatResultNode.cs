namespace NodeGraph.Model;

[Node]
public partial class FloatResultNode
{
    [Input] private float _value;
    public float Value => _value;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
