using System.Runtime.InteropServices.JavaScript;

namespace NodeGraph.Web.Storage;

/// <summary>
/// ブラウザのlocalStorageを使用するストレージプロバイダー
/// </summary>
public partial class BrowserStorageProvider : IStorageProvider
{
    public Task<string?> ReadTextAsync(string key)
    {
        var value = LocalStorageGetItem(key);
        return Task.FromResult(value);
    }

    public Task WriteTextAsync(string key, string content)
    {
        LocalStorageSetItem(key, content);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        var value = LocalStorageGetItem(key);
        return Task.FromResult(value != null);
    }

    public Task DeleteAsync(string key)
    {
        LocalStorageRemoveItem(key);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> ListKeysAsync(string prefix = "")
    {
        var keys = new List<string>();
        var length = GetLocalStorageLength();

        for (var i = 0; i < length; i++)
        {
            var key = LocalStorageKey(i);
            if (key != null && (string.IsNullOrEmpty(prefix) || key.StartsWith(prefix)))
            {
                keys.Add(key);
            }
        }

        return Task.FromResult<IEnumerable<string>>(keys);
    }

    public Task DownloadFileAsync(string filename, string content)
    {
        DownloadFile(filename, content);
        return Task.CompletedTask;
    }

    public async Task<(string filename, string content)?> UploadFileAsync()
    {
        var result = await UploadFileFromInput();
        if (string.IsNullOrEmpty(result))
        {
            return null;
        }

        // Format: "filename\n---CONTENT---\ncontent"
        var separatorIndex = result.IndexOf("\n---CONTENT---\n", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return null;
        }

        var filename = result[..separatorIndex];
        var content = result[(separatorIndex + 15)..];
        return (filename, content);
    }

    #region JavaScript Interop

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? LocalStorageGetItem(string key);

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void LocalStorageSetItem(string key, string value);

    [JSImport("globalThis.localStorage.removeItem")]
    private static partial void LocalStorageRemoveItem(string key);

    [JSImport("globalThis.localStorage.key")]
    private static partial string? LocalStorageKey(int index);

    [JSImport("globalThis.getLocalStorageLength")]
    private static partial int GetLocalStorageLength();

    [JSImport("globalThis.downloadFile")]
    private static partial void DownloadFile(string filename, string content);

    [JSImport("globalThis.uploadFile")]
    private static partial Task<string> UploadFileFromInput();

    #endregion
}
