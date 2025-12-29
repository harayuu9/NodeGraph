namespace NodeGraph.Model;

[Node("String Concat", "String")]
public partial class StringConcatNode
{
    [Input] private string _a = string.Empty;
    [Input] private string _b = string.Empty;
    [Output] private string _result = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = (_a ?? string.Empty) + (_b ?? string.Empty);
        await context.ExecuteOutAsync(0);
    }
}
