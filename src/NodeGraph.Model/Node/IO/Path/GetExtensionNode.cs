namespace NodeGraph.Model;

/// <summary>
/// パスから拡張子を取得するノード
/// </summary>
[Node("Get Extension", "IO/Path")]
public partial class GetExtensionNode
{
    [Input] private string _path = string.Empty;
    [Output] private string _extension = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _extension = Path.GetExtension(_path);
        await context.ExecuteOutAsync(0);
    }
}
