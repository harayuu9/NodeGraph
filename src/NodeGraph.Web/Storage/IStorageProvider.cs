namespace NodeGraph.Web.Storage;

/// <summary>
/// ストレージプロバイダーのインターフェース。
/// デスクトップではファイルシステム、Webではブラウザストレージを使用。
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// 指定されたキーのテキストデータを読み込む
    /// </summary>
    Task<string?> ReadTextAsync(string key);

    /// <summary>
    /// 指定されたキーにテキストデータを書き込む
    /// </summary>
    Task WriteTextAsync(string key, string content);

    /// <summary>
    /// 指定されたキーが存在するかどうかを確認する
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 指定されたキーを削除する
    /// </summary>
    Task DeleteAsync(string key);

    /// <summary>
    /// 指定されたプレフィックスで始まるキーの一覧を取得する
    /// </summary>
    Task<IEnumerable<string>> ListKeysAsync(string prefix = "");

    /// <summary>
    /// ファイルをダウンロードする（Web用）
    /// </summary>
    Task DownloadFileAsync(string filename, string content);

    /// <summary>
    /// ファイルをアップロードする（Web用）
    /// </summary>
    Task<(string filename, string content)?> UploadFileAsync();
}
