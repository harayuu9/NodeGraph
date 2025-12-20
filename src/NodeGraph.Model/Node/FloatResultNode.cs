namespace NodeGraph.Model;

[Node(HasExecIn = false, HasExecOut = false)]
public partial class FloatResultNode
{
    [Input] private float _value;
    public float Value => _value;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}

[Node(HasExecIn = false, HasExecOut = false)]
public partial class PreviewNode
{
    [Input] private object? _value;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}