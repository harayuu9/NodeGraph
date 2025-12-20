namespace NodeGraph.Model;

[Node]
public partial class FloatResultNode
{
    [Input] private float _value;
    public float Value => _value;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        await context.ExecuteOutAsync(0);
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