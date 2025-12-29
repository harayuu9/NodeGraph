namespace NodeGraph.Model;

/// <summary>
/// テキストファイルに書き込むノード
/// </summary>
[Node("Write Text File", "IO/File")]
public partial class FileWriteTextNode
{
    [Input] private string _path = string.Empty;
    [Input] private string _content = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        await File.WriteAllTextAsync(_path, _content, context.CancellationToken);
        await context.ExecuteOutAsync(0);
    }
}
