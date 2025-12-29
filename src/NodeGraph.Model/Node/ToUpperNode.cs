namespace NodeGraph.Model;

[Node("To Upper", "String")]
public partial class ToUpperNode
{
    [Input] private string _input = string.Empty;
    [Output] private string _result = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = (_input ?? string.Empty).ToUpperInvariant();
        await context.ExecuteOutAsync(0);
    }
}
