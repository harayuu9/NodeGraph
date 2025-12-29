namespace NodeGraph.Model;

[Node("String Format", "String")]
public partial class StringFormatNode
{
    [Property(DisplayName = "Format", Tooltip = "フォーマット文字列")]
    private string _format = "{0}";

    [Input] private string _arg0 = string.Empty;
    [Input] private string _arg1 = string.Empty;
    [Input] private string _arg2 = string.Empty;
    [Output] private string _result = string.Empty;

    public void SetFormat(string format)
    {
        _format = format;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        try
        {
            _result = string.Format(
                _format ?? "{0}",
                _arg0 ?? string.Empty,
                _arg1 ?? string.Empty,
                _arg2 ?? string.Empty);
        }
        catch (FormatException)
        {
            _result = _format ?? string.Empty;
        }

        await context.ExecuteOutAsync(0);
    }
}
