namespace NodeGraph.Model;

[Node("Replace", "String")]
public partial class ReplaceNode
{
    [Input] private string _input = string.Empty;
    [Input] private string _oldValue = string.Empty;
    [Input] private string _newValue = string.Empty;
    [Output] private string _result = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;
        var oldValue = _oldValue ?? string.Empty;
        var newValue = _newValue ?? string.Empty;

        if (string.IsNullOrEmpty(oldValue))
        {
            _result = input;
        }
        else
        {
            _result = input.Replace(oldValue, newValue);
        }

        await context.ExecuteOutAsync(0);
    }
}
