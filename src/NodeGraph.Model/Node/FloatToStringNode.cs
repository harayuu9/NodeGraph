namespace NodeGraph.Model;

[Node("Float To String", "Convert")]
public partial class FloatToStringNode
{
    [Input] private float _input;

    [Property(DisplayName = "Format", Tooltip = "数値フォーマット (例: F2, N2, E3)")]
    private string _format = "F2";

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
