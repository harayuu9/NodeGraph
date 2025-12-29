namespace NodeGraph.Model;

/// <summary>
/// ファイルの存在を確認するノード
/// </summary>
[Node("File Exists", "IO/File", HasExecIn = false, HasExecOut = false)]
public partial class FileExistsNode
{
    [Input] private string _path = string.Empty;
    [Output] private bool _exists;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _exists = File.Exists(_path);
        return Task.CompletedTask;
    }
}
