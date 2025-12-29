using Microsoft.Extensions.AI;

namespace NodeGraph.Model.AI;

/// <summary>
/// ユーザーメッセージをChatHistoryに追加するノード。
/// ユーザーからの入力メッセージを追加します。
/// </summary>
[Node("Add User Message", "AI/History", "Out")]
public partial class AddUserMessageNode
{
    [Input]
    private IList<ChatMessage> _inputHistory = new List<ChatMessage>();

    [Input]
    private string _content = string.Empty;

    [Output]
    private IList<ChatMessage> _outputHistory = new List<ChatMessage>();

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var history = new List<ChatMessage>();

        foreach (var msg in _inputHistory)
        {
            history.Add(msg);
        }

        if (!string.IsNullOrEmpty(_content))
        {
            history.Add(new ChatMessage(ChatRole.User, _content));
        }

        _outputHistory = history;
        await context.ExecuteOutAsync(0);
    }
}
