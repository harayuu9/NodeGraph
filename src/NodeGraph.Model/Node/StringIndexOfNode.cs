namespace NodeGraph.Model;

[Node("String IndexOf", "String")]
public partial class StringIndexOfNode
{
    [Input] private string _input = string.Empty;
    [Input] private string _search = string.Empty;

    [Property(DisplayName = "Ignore Case", Tooltip = "大文字小文字を無視するか")]
    private bool _ignoreCase;

    [Output] private int _index;
    [Output] private bool _found;

    public void SetIgnoreCase(bool ignoreCase)
    {
        _ignoreCase = ignoreCase;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;
        var search = _search ?? string.Empty;

        var comparison = _ignoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        _index = input.IndexOf(search, comparison);
        _found = _index >= 0;

        await context.ExecuteOutAsync(0);
    }
}
