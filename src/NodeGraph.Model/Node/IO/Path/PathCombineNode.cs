namespace NodeGraph.Model;

/// <summary>
/// 2つのパスを結合するノード
/// </summary>
[Node("Combine Path", "IO/Path")]
public partial class PathCombineNode
{
    [Input] private string _path1 = string.Empty;
    [Input] private string _path2 = string.Empty;
    [Output] private string _result = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = Path.Combine(_path1, _path2);
        await context.ExecuteOutAsync(0);
    }
}
