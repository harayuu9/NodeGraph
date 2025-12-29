using Microsoft.Extensions.AI;

namespace NodeGraph.Model.AI;

/// <summary>
/// アシスタントメッセージをChatHistoryに追加するノード。
/// AIからの応答や過去の会話を再構築する際に使用します。
/// </summary>
[Node("Add Assistant Message", "AI/History", HasExecIn = false, HasExecOut = false)]
public partial class AddAssistantMessageNode
{
    [Input]
    private IList<ChatMessage> _inputHistory = new List<ChatMessage>();

    [Input]
    private string _content = string.Empty;

    [Output]
    private IList<ChatMessage> _outputHistory = new List<ChatMessage>();

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var history = new List<ChatMessage>();

        foreach (var msg in _inputHistory)
        {
            history.Add(msg);
        }

        if (!string.IsNullOrEmpty(_content))
        {
            history.Add(new ChatMessage(ChatRole.Assistant, _content));
        }

        _outputHistory = history;
        return Task.CompletedTask;
    }
}
