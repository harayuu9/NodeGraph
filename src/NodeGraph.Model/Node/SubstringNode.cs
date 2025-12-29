namespace NodeGraph.Model;

[Node("Substring", "String")]
public partial class SubstringNode
{
    [Input] private string _input = string.Empty;
    [Input] private int _startIndex;
    [Input] private int _length = -1;
    [Output] private string _result = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;

        if (_startIndex < 0 || _startIndex >= input.Length)
        {
            _result = string.Empty;
        }
        else if (_length < 0)
        {
            _result = input.Substring(_startIndex);
        }
        else
        {
            var actualLength = Math.Min(_length, input.Length - _startIndex);
            _result = input.Substring(_startIndex, actualLength);
        }

        await context.ExecuteOutAsync(0);
    }
}
