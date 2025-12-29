namespace NodeGraph.Model;

/// <summary>
/// テキストファイルに追記するノード
/// </summary>
[Node("Append Text File", "IO/File")]
public partial class FileAppendTextNode
{
    [Input] private string _path = string.Empty;
    [Input] private string _content = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        await File.AppendAllTextAsync(_path, _content, context.CancellationToken);
        await context.ExecuteOutAsync(0);
    }
}
