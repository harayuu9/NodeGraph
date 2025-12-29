namespace NodeGraph.Model;

/// <summary>
/// ディレクトリの存在を確認するノード
/// </summary>
[Node("Directory Exists", "IO/Directory", HasExecIn = false, HasExecOut = false)]
public partial class DirectoryExistsNode
{
    [Input] private string _path = string.Empty;
    [Output] private bool _exists;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _exists = System.IO.Directory.Exists(_path);
        return Task.CompletedTask;
    }
}
