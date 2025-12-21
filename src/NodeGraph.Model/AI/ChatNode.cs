using Microsoft.Extensions.AI;

namespace NodeGraph.Model.AI;

/// <summary>
/// AIチャットノード。プロンプトを送信してAIからの応答を取得します。
/// </summary>
[Node("Chat", "AI", "Out")]
public partial class ChatNode
{
    [Input]
    private string _prompt = string.Empty;

    [Output]
    private string _response = string.Empty;

    [Property(DisplayName = "システムプロンプト")]
    private string _systemPrompt = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var chatClient = context.GetService<IChatClient>();

        if (chatClient == null)
        {
            _response = "[Error] IChatClient is not registered. Please set OPENAI_API_KEY parameter.";
            await context.ExecuteOutAsync(0);
            return;
        }

        var messages = new List<ChatMessage>();

        // システムプロンプトがあれば追加
        if (!string.IsNullOrEmpty(_systemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, _systemPrompt));
        }

        // ユーザープロンプトを追加
        messages.Add(new ChatMessage(ChatRole.User, _prompt));

        // AIからの応答を取得
        var response = await chatClient.GetResponseAsync(messages, cancellationToken: context.CancellationToken);
        _response = response.Text;

        await context.ExecuteOutAsync(0);
    }
}
