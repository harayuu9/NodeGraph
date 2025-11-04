namespace NodeGraph.Model;

[Node]
public partial class FloatConstantNode
{
    [Property(DisplayName = "Value", Tooltip = "定数値")]
    [Range(0, 100)]
    [Output]
    private float _value;

    public void SetValue(float value)
    {
        _value = value;
    }

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
