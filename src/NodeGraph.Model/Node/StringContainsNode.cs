namespace NodeGraph.Model;

[Node("String Contains", "String")]
public partial class StringContainsNode
{
    [Input] private string _input = string.Empty;
    [Input] private string _search = string.Empty;

    [Property(DisplayName = "Ignore Case", Tooltip = "大文字小文字を無視するか")]
    private bool _ignoreCase;

    [Output] private bool _result;

    public void SetIgnoreCase(bool ignoreCase)
    {
        _ignoreCase = ignoreCase;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;
        var search = _search ?? string.Empty;

        if (string.IsNullOrEmpty(search))
        {
            _result = true;
        }
        else
        {
            var comparison = _ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            _result = input.Contains(search, comparison);
        }

        await context.ExecuteOutAsync(0);
    }
}
