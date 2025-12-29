namespace NodeGraph.Model;

/// <summary>
/// バイナリファイルを読み込むノード
/// </summary>
[Node("Read Bytes File", "IO/File")]
public partial class FileReadBytesNode
{
    [Input] private string _path = string.Empty;
    [Output] private byte[] _data = [];

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _data = await File.ReadAllBytesAsync(_path, context.CancellationToken);
        await context.ExecuteOutAsync(0);
    }
}
