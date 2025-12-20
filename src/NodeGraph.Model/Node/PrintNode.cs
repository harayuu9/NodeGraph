namespace NodeGraph.Model;

/// <summary>
/// デバッグ用のPrintノード。値をコンソールに出力します。
/// </summary>
[Node("Print", "Debug", "Out")]
public partial class PrintNode
{
    [Input]
    private string _message = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        Console.WriteLine($"[PrintNode] {_message}");
        await context.ExecuteOutAsync(0);
    }
}
