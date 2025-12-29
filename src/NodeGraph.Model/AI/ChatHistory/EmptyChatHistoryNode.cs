using Microsoft.Extensions.AI;

namespace NodeGraph.Model.AI;

/// <summary>
/// 空のChatHistoryを生成するノード。
/// 会話履歴チェーンの開始点として使用します。
/// </summary>
[Node("Empty Chat History", "AI/History", HasExecIn = false, HasExecOut = false)]
public partial class EmptyChatHistoryNode
{
    [Output]
    private IList<ChatMessage> _history = new List<ChatMessage>();

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _history = new List<ChatMessage>();
        return Task.CompletedTask;
    }
}
