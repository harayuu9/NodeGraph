using Microsoft.Extensions.AI;

namespace NodeGraph.Model.AI;

/// <summary>
/// ChatHistoryのメッセージ数を取得するノード。
/// </summary>
[Node("Chat History Length", "AI/History", HasExecIn = false, HasExecOut = false)]
public partial class ChatHistoryLengthNode
{
    [Input]
    private IList<ChatMessage> _history = new List<ChatMessage>();

    [Output]
    private int _count;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _count = _history.Count;
        return Task.CompletedTask;
    }
}
