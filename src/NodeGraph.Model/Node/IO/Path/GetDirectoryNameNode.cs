namespace NodeGraph.Model;

/// <summary>
/// パスからディレクトリ名を取得するノード
/// </summary>
[Node("Get Directory Name", "IO/Path")]
public partial class GetDirectoryNameNode
{
    [Input] private string _path = string.Empty;
    [Output] private string _directoryName = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _directoryName = Path.GetDirectoryName(_path) ?? string.Empty;
        await context.ExecuteOutAsync(0);
    }
}
