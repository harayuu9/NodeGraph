using Microsoft.Extensions.AI;

namespace NodeGraph.Model.AI;

/// <summary>
/// ChatHistoryから最後のアシスタント応答を取得するノード。
/// </summary>
[Node("Get Last Response", "AI/History", "Out")]
public partial class GetLastResponseNode
{
    [Input]
    private IList<ChatMessage> _history = new List<ChatMessage>();

    [Output]
    private string _response = string.Empty;

    [Output]
    private bool _found;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _response = string.Empty;
        _found = false;

        if (_history.Count == 0)
        {
            await context.ExecuteOutAsync(0);
            return;
        }

        // 最後のアシスタントメッセージを検索
        for (int i = _history.Count - 1; i >= 0; i--)
        {
            if (_history[i].Role == ChatRole.Assistant)
            {
                _response = _history[i].Text ?? string.Empty;
                _found = true;
                break;
            }
        }

        await context.ExecuteOutAsync(0);
    }
}
