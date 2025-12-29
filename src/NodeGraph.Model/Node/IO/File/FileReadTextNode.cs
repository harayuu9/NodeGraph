namespace NodeGraph.Model;

/// <summary>
/// テキストファイルを読み込むノード
/// </summary>
[Node("Read Text File", "IO/File")]
public partial class FileReadTextNode
{
    [Input] private string _path = string.Empty;
    [Output] private string _content = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _content = await File.ReadAllTextAsync(_path, context.CancellationToken);
        await context.ExecuteOutAsync(0);
    }
}
