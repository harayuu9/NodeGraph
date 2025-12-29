using System.Net.Http;
using System.Text;

namespace NodeGraph.Model;

/// <summary>
/// HTTP POSTリクエストを送信するノード（JSON body対応）
/// </summary>
[Node("HTTP POST", "IO/Http")]
public partial class HttpPostNode
{
    private static readonly HttpClient HttpClient = new();

    [Input] private string _url = string.Empty;
    [Input] private string _body = string.Empty;
    [Property(DisplayName = "Content Type", Tooltip = "リクエストボディのContent-Type")]
    private string _contentType = "application/json";
    [Output] private string _response = string.Empty;
    [Output] private int _statusCode;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        using var content = new StringContent(_body, Encoding.UTF8, _contentType);
        using var response = await HttpClient.PostAsync(_url, content, context.CancellationToken);
        _statusCode = (int)response.StatusCode;
#if NETSTANDARD2_1
        _response = await response.Content.ReadAsStringAsync();
#else
        _response = await response.Content.ReadAsStringAsync(context.CancellationToken);
#endif
        await context.ExecuteOutAsync(0);
    }
}
