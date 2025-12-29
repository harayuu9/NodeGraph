namespace NodeGraph.Model;

[Node("String Length", "String")]
public partial class StringLengthNode
{
    [Input] private string _input = string.Empty;
    [Output] private int _length;
    [Output] private bool _isEmpty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;
        _length = input.Length;
        _isEmpty = input.Length == 0;
        await context.ExecuteOutAsync(0);
    }
}
