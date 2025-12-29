namespace NodeGraph.Model;

[Node("String To Float", "Convert")]
public partial class StringToFloatNode
{
    [Input] private string _input = string.Empty;

    [Property(DisplayName = "Default", Tooltip = "パース失敗時のデフォルト値")]
    private float _defaultValue;

    [Output] private float _result;
    [Output] private bool _success;

    public void SetDefaultValue(float defaultValue)
    {
        _defaultValue = defaultValue;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;

        if (float.TryParse(input, out var value))
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
