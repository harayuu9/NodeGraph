using Microsoft.Extensions.AI;

namespace NodeGraph.Model.AI;

/// <summary>
/// 画像付きユーザーメッセージをChatHistoryに追加するノード。
/// マルチモーダルAIモデル（GPT-4 Visionなど）で使用します。
/// </summary>
[Node("Add User Image Message", "AI/History", HasExecIn = false, HasExecOut = false)]
public partial class AddUserImageMessageNode
{
    [Input]
    private IList<ChatMessage> _inputHistory = new List<ChatMessage>();

    [Input]
    private string _textContent = string.Empty;

    [Input]
    private string _imageUrl = string.Empty;

    [Property(DisplayName = "Media Type")]
    private string _mediaType = "image/png";

    [Output]
    private IList<ChatMessage> _outputHistory = new List<ChatMessage>();

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var history = new List<ChatMessage>();

        foreach (var msg in _inputHistory)
        {
            history.Add(msg);
        }

        // マルチモーダルコンテンツを作成
        var contents = new List<AIContent>();

        if (!string.IsNullOrEmpty(_textContent))
        {
            contents.Add(new TextContent(_textContent));
        }

        if (!string.IsNullOrEmpty(_imageUrl))
        {
            // Data URIまたはURLを判定
            if (_imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                // Base64データURIの場合
                contents.Add(new DataContent(_imageUrl));
            }
            else
            {
                // URLの場合
                contents.Add(new DataContent(new Uri(_imageUrl), _mediaType));
            }
        }

        if (contents.Count > 0)
        {
            history.Add(new ChatMessage(ChatRole.User, contents));
        }

        _outputHistory = history;
        return Task.CompletedTask;
    }
}
