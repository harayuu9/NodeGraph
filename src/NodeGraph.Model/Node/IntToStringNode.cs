namespace NodeGraph.Model;

[Node("Int To String", "Convert")]
public partial class IntToStringNode
{
    [Input] private int _input;

    [Property(DisplayName = "Format", Tooltip = "数値フォーマット (例: D5, N0, X)")]
    private string _format = string.Empty;

    [Output] private string _result = string.Empty;

    public void SetFormat(string format)
    {
        _format = format;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = string.IsNullOrEmpty(_format)
            ? _input.ToString()
            : _input.ToString(_format);

        await context.ExecuteOutAsync(0);
    }
}
