namespace NodeGraph.Model;

[Node("String Split", "String")]
public partial class StringSplitNode
{
    [Property(DisplayName = "Separator", Tooltip = "分割時の区切り文字")]
    private string _separator = ",";

    [Input] private string _input = string.Empty;
    [Output] private string _first = string.Empty;
    [Output] private string _second = string.Empty;
    [Output] private int _count;

    public void SetSeparator(string separator)
    {
        _separator = separator;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;
        var sep = _separator ?? string.Empty;

        if (string.IsNullOrEmpty(sep))
        {
            _first = input;
            _second = string.Empty;
            _count = string.IsNullOrEmpty(input) ? 0 : 1;
        }
        else
        {
            var parts = input.Split(new[] { sep }, StringSplitOptions.None);
            _first = parts.Length > 0 ? parts[0] : string.Empty;
            _second = parts.Length > 1 ? parts[1] : string.Empty;
            _count = parts.Length;
        }

        await context.ExecuteOutAsync(0);
    }
}
