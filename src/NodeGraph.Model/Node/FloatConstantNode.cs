namespace NodeGraph.Model;

[Node("Float", "Constant", HasExecIn = false, HasExecOut = false)]
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

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}

[Node("Int", "Constant", HasExecIn = false, HasExecOut = false)]
public partial class IntConstantNode
{
    [Property(DisplayName = "Value", Tooltip = "定数値")]
    [Range(0, 100)]
    [Output]
    private int _value;

    public void SetValue(int value)
    {
        _value = value;
    }

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}
