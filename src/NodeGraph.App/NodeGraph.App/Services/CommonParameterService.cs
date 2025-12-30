using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NodeGraph.App.Services;

/// <summary>
/// 共通パラメータのシリアライズ用データ構造
/// </summary>
public class CommonParameterData
{
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, object?> Parameters { get; set; } = [];
}

/// <summary>
/// 全てのグラフ実行に適用される共通パラメータを管理するサービス。
/// 設定ファイル（AppData）と環境変数の両方からパラメータを読み込みます。
/// </summary>
public class CommonParameterService
{
    private const string SettingsFileName = "common-parameters.yml";
    private const string EnvVarPrefix = "NODEGRAPH_PARAM_";

    private readonly string _settingsFilePath;
    private Dictionary<string, object?> _fileParameters = [];

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public CommonParameterService()
    {
        // %AppData%/NodeGraph/common-parameters.yml に保存
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDir = Path.Combine(appDataPath, "NodeGraph");
        Directory.CreateDirectory(settingsDir);
        _settingsFilePath = Path.Combine(settingsDir, SettingsFileName);

        LoadFromFile();
    }

    /// <summary>
    /// 設定ファイルのパスを取得します。
    /// </summary>
    public string SettingsFilePath => _settingsFilePath;

    /// <summary>
    /// マージされたパラメータを取得します（環境変数がファイル設定を上書き）。
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetParameters()
    {
        var merged = new Dictionary<string, object?>(_fileParameters);

        // 環境変数で上書き（NODEGRAPH_PARAM_ プレフィックス付き）
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (key != null && key.StartsWith(EnvVarPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var paramName = key.Substring(EnvVarPrefix.Length);
                merged[paramName] = entry.Value?.ToString();
            }
        }

        return merged;
    }

    /// <summary>
    /// ファイルに保存されたパラメータのみを取得します（環境変数を除く）。
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetFileParameters()
    {
        return _fileParameters;
    }

    /// <summary>
    /// パラメータを設定ファイルに設定します。
    /// </summary>
    public void SetParameter(string name, object? value)
    {
        _fileParameters[name] = value;
        SaveToFile();
    }

    /// <summary>
    /// パラメータを設定ファイルから削除します。
    /// </summary>
    public void RemoveParameter(string name)
    {
        _fileParameters.Remove(name);
        SaveToFile();
    }

    /// <summary>
    /// 設定ファイルの全パラメータをクリアします。
    /// </summary>
    public void ClearParameters()
    {
        _fileParameters.Clear();
        SaveToFile();
    }

    /// <summary>
    /// 設定ファイルを再読み込みします。
    /// </summary>
    public void Reload()
    {
        LoadFromFile();
    }

    private void LoadFromFile()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var yaml = File.ReadAllText(_settingsFilePath);
                var data = YamlDeserializer.Deserialize<CommonParameterData>(yaml);
                _fileParameters = data?.Parameters ?? [];
            }
            catch
            {
                _fileParameters = [];
            }
        }
    }

    private void SaveToFile()
    {
        var data = new CommonParameterData { Parameters = _fileParameters };
        var yaml = YamlSerializer.Serialize(data);
        File.WriteAllText(_settingsFilePath, yaml);
    }
}
