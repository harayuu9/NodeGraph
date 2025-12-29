namespace NodeGraph.Model;

[Node("String Join", "String")]
public partial class StringJoinNode
{
    [Property(DisplayName = "Separator", Tooltip = "結合時の区切り文字")]
    private string _separator = string.Empty;

    [Input] private string _a = string.Empty;
    [Input] private string _b = string.Empty;
    [Output] private string _result = string.Empty;

    public void SetSeparator(string separator)
    {
        _separator = separator;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = string.Join(_separator ?? string.Empty, _a ?? string.Empty, _b ?? string.Empty);
        await context.ExecuteOutAsync(0);
    }
}
