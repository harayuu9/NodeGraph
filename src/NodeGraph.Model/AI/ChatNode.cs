using Microsoft.Extensions.AI;

namespace NodeGraph.Model.AI;

/// <summary>
/// AIチャットノード。ChatHistoryを入力として受け取り、AIからの応答を取得します。
/// 出力には応答テキストと、応答を含む更新されたChatHistoryが含まれます。
/// </summary>
[Node("Chat", "AI", "Out")]
public partial class ChatNode
{
    [Input]
    private IList<ChatMessage> _chatHistory = new List<ChatMessage>();

    [Output]
    private string _response = string.Empty;

    [Output]
    private IList<ChatMessage> _outputHistory = new List<ChatMessage>();

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var messages = new List<ChatMessage>(_chatHistory);

        // 空の履歴の場合は早期リターン（IChatClient不要）
        if (messages.Count == 0)
        {
            _response = string.Empty;
            _outputHistory = new List<ChatMessage>();
            await context.ExecuteOutAsync(0);
            return;
        }

        var chatClient = context.GetService<IChatClient>();

        if (chatClient == null)
        {
            _response = "[Error] IChatClient is not registered. Please set OPENAI_API_KEY parameter.";
            _outputHistory = new List<ChatMessage>();
            await context.ExecuteOutAsync(0);
            return;
        }

        // AIからの応答を取得
        var response = await chatClient.GetResponseAsync(messages, cancellationToken: context.CancellationToken);
        _response = response.Text ?? string.Empty;

        // 出力履歴に応答を追加
        _outputHistory = new List<ChatMessage>(messages);
        _outputHistory.Add(new ChatMessage(ChatRole.Assistant, _response));

        await context.ExecuteOutAsync(0);
    }
}
