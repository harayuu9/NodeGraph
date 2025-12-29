namespace NodeGraph.Model;

/// <summary>
/// バイナリファイルに書き込むノード
/// </summary>
[Node("Write Bytes File", "IO/File")]
public partial class FileWriteBytesNode
{
    [Input] private string _path = string.Empty;
    [Input] private byte[] _data = [];

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        await File.WriteAllBytesAsync(_path, _data, context.CancellationToken);
        await context.ExecuteOutAsync(0);
    }
}
