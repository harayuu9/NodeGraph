namespace NodeGraph.Model;

/// <summary>
/// ディレクトリを作成するノード
/// </summary>
[Node("Create Directory", "IO/Directory")]
public partial class CreateDirectoryNode
{
    [Input] private string _path = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        System.IO.Directory.CreateDirectory(_path);
        await context.ExecuteOutAsync(0);
    }
}
