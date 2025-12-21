using Microsoft.Extensions.AI;
using NodeGraph.Model.Services;
using OpenAI.Chat;

namespace NodeGraph.Model.AI;

/// <summary>
/// OpenAI ChatClientを初期化するInitializer。
/// パラメータ "OPENAI_API_KEY" または環境変数からAPIキーを取得します。
/// パラメータ "OPENAI_MODEL" でモデルを指定できます（デフォルト: GPT-5.2）。
/// </summary>
public class OpenAIChatClientInitializer : INodeContextInitializer
{
    /// <summary>
    /// APIキーのパラメータ名。
    /// </summary>
    public const string ApiKeyParameterName = "OPENAI_API_KEY";

    /// <summary>
    /// モデル名のパラメータ名。
    /// </summary>
    public const string ModelParameterName = "OPENAI_MODEL";

    /// <summary>
    /// デフォルトのモデル名。
    /// </summary>
    public const string DefaultModel = "gpt-5.2";

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public void Initialize(IInitializerContext context)
    {
        // APIキーを取得（パラメータ優先、なければ環境変数）
        var apiKey = context.GetParameter<string>(ApiKeyParameterName) ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            // APIキーがない場合は登録しない（AI機能は使用不可）
            return;
        }

        // モデル名を取得
        var model = context.GetParameter<string>(ModelParameterName) ?? DefaultModel;

        // OpenAI ChatClientを作成してIChatClientとして登録
        var openAIChatClient = new ChatClient(model, apiKey);
        var chatClient = openAIChatClient.AsIChatClient();

        context.Register(chatClient);
    }
}
