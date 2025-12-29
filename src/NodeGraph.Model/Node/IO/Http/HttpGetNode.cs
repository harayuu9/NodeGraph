using System.Net.Http;

namespace NodeGraph.Model;

/// <summary>
/// HTTP GETリクエストを送信するノード
/// </summary>
[Node("HTTP GET", "IO/Http")]
public partial class HttpGetNode
{
    private static readonly HttpClient HttpClient = new();

    [Input] private string _url = string.Empty;
    [Output] private string _response = string.Empty;
    [Output] private int _statusCode;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        using var response = await HttpClient.GetAsync(_url, context.CancellationToken);
        _statusCode = (int)response.StatusCode;
#if NETSTANDARD2_1
        _response = await response.Content.ReadAsStringAsync();
#else
        _response = await response.Content.ReadAsStringAsync(context.CancellationToken);
#endif
        await context.ExecuteOutAsync(0);
    }
}
