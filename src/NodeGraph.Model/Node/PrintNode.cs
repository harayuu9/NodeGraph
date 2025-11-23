namespace NodeGraph.Model;

/// <summary>
/// デバッグ用のPrintノード。値をコンソールに出力します。
/// </summary>
[ExecutionNode("Print", "Debug", "Out")]
public partial class PrintNode
{
    [Input]
    private string _message = string.Empty;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        Console.WriteLine($"[PrintNode] {_message}");
        return Task.CompletedTask;
    }
}
