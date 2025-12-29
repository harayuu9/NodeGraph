using System.Text.Json;
using NodeGraph.Web.Storage;

namespace NodeGraph.Web.Services;

/// <summary>
/// ブラウザ用のパラメータサービス。localStorageを使用してパラメータを保存。
/// </summary>
public class BrowserParameterService
{
    private const string StorageKey = "nodegraph:parameters";
    private const string PresetsPrefix = "nodegraph:preset:";
    private const string PresetsListKey = "nodegraph:presets";
    private readonly IStorageProvider _storage;
    private Dictionary<string, object?> _parameters = [];
    private List<string> _presetNames = [];

    public BrowserParameterService(IStorageProvider storage)
    {
        _storage = storage;
        _ = LoadAsync();
        _ = LoadPresetsListAsync();
    }

    /// <summary>
    /// パラメータを取得
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetParameters()
    {
        return _parameters;
    }

    /// <summary>
    /// パラメータを設定
    /// </summary>
    public async Task SetParameterAsync(string name, object? value)
    {
        _parameters[name] = value;
        await SaveAsync();
    }

    /// <summary>
    /// パラメータを削除
    /// </summary>
    public async Task RemoveParameterAsync(string name)
    {
        _parameters.Remove(name);
        await SaveAsync();
    }

    /// <summary>
    /// 再読み込み
    /// </summary>
    public async Task ReloadAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var json = await _storage.ReadTextAsync(StorageKey);
        if (string.IsNullOrEmpty(json))
        {
            _parameters = [];
            return;
        }

        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            _parameters = data ?? [];
        }
        catch
        {
            _parameters = [];
        }
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_parameters);
        await _storage.WriteTextAsync(StorageKey, json);
    }

    // ===== プリセット機能 =====

    /// <summary>
    /// プリセット一覧を取得
    /// </summary>
    public IReadOnlyList<string> GetPresetNames() => _presetNames;

    /// <summary>
    /// 現在のパラメータをプリセットとして保存
    /// </summary>
    public async Task SavePresetAsync(string name)
    {
        var json = JsonSerializer.Serialize(_parameters);
        await _storage.WriteTextAsync($"{PresetsPrefix}{name}", json);

        if (!_presetNames.Contains(name))
        {
            _presetNames.Add(name);
            await SavePresetsListAsync();
        }
    }

    /// <summary>
    /// プリセットを読み込んで現在のパラメータに適用
    /// </summary>
    public async Task LoadPresetAsync(string name)
    {
        var json = await _storage.ReadTextAsync($"{PresetsPrefix}{name}");
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            if (data != null)
            {
                _parameters = data;
                await SaveAsync();
            }
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// プリセットを削除
    /// </summary>
    public async Task DeletePresetAsync(string name)
    {
        await _storage.DeleteAsync($"{PresetsPrefix}{name}");
        _presetNames.Remove(name);
        await SavePresetsListAsync();
    }

    /// <summary>
    /// プリセット一覧を再読み込み
    /// </summary>
    public async Task ReloadPresetsAsync()
    {
        await LoadPresetsListAsync();
    }

    private async Task LoadPresetsListAsync()
    {
        var json = await _storage.ReadTextAsync(PresetsListKey);
        if (string.IsNullOrEmpty(json))
        {
            _presetNames = [];
            return;
        }

        try
        {
            var data = JsonSerializer.Deserialize<List<string>>(json);
            _presetNames = data ?? [];
        }
        catch
        {
            _presetNames = [];
        }
    }

    private async Task SavePresetsListAsync()
    {
        var json = JsonSerializer.Serialize(_presetNames);
        await _storage.WriteTextAsync(PresetsListKey, json);
    }
}
