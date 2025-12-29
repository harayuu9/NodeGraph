namespace NodeGraph.Model;

[Node("String To Int", "Convert")]
public partial class StringToIntNode
{
    [Input] private string _input = string.Empty;

    [Property(DisplayName = "Default", Tooltip = "パース失敗時のデフォルト値")]
    private int _defaultValue;

    [Output] private int _result;
    [Output] private bool _success;

    public void SetDefaultValue(int defaultValue)
    {
        _defaultValue = defaultValue;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;

        if (int.TryParse(input, out var value))
        {
            _result = value;
            _success = true;
        }
        else
        {
            _result = _defaultValue;
            _success = false;
        }

        await context.ExecuteOutAsync(0);
    }
}
