namespace NodeGraph.Model;

/// <summary>
/// パスからファイル名を取得するノード
/// </summary>
[Node("Get File Name", "IO/Path")]
public partial class GetFileNameNode
{
    [Input] private string _path = string.Empty;
    [Output] private string _fileName = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _fileName = Path.GetFileName(_path);
        await context.ExecuteOutAsync(0);
    }
}
