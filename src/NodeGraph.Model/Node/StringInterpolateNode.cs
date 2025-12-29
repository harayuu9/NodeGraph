namespace NodeGraph.Model;

[Node("String Interpolate", "String")]
public partial class StringInterpolateNode
{
    [Property(DisplayName = "Template", Tooltip = "テンプレート文字列")]
    private string _template = "{A}";

    [Input] private string _a = string.Empty;
    [Input] private string _b = string.Empty;
    [Input] private string _c = string.Empty;
    [Output] private string _result = string.Empty;

    public void SetTemplate(string template)
    {
        _template = template;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var result = _template ?? string.Empty;
        result = result.Replace("{A}", _a ?? string.Empty);
        result = result.Replace("{B}", _b ?? string.Empty);
        result = result.Replace("{C}", _c ?? string.Empty);
        _result = result;

        await context.ExecuteOutAsync(0);
    }
}
