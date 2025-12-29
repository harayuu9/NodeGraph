namespace NodeGraph.Model;

[Node("To Lower", "String")]
public partial class ToLowerNode
{
    [Input] private string _input = string.Empty;
    [Output] private string _result = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = (_input ?? string.Empty).ToLowerInvariant();
        await context.ExecuteOutAsync(0);
    }
}
