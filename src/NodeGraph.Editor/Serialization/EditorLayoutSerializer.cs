using System;
using System.IO;
using NodeGraph.Editor.Models;
using NodeGraph.Model.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NodeGraph.Editor.Serialization;

/// <summary>
/// エディタレイアウト（ノード位置など）のシリアライズ/デシリアライズを行うクラス
/// </summary>
public static class EditorLayoutSerializer
{
    private const string CurrentVersion = "1.0.0";

    /// <summary>
    /// レイアウトをYAMLファイルに保存します
    /// </summary>
    public static void SaveLayoutToFile(EditorGraph editorGraph, string filePath)
    {
        File.WriteAllText(filePath, SaveLayout(editorGraph));
    }

    public static string SaveLayout(EditorGraph editorGraph)
    {
        var layoutData = SerializeLayout(editorGraph);
        return YamlSerializer.Serialize(layoutData);
    }

    /// <summary>
    /// YAMLファイルからレイアウトを読み込みます
    /// </summary>
    public static void LoadLayoutFromFile(string filePath, EditorGraph editorGraph)
    {
        if (!File.Exists(filePath))
            // レイアウトファイルが存在しない場合はデフォルト配置
            return;

        var yaml = File.ReadAllText(filePath);
        LoadLayout(yaml, editorGraph);
    }

    public static void LoadLayout(string yaml, EditorGraph editorGraph)
    {
        var layoutData = YamlSerializer.Deserialize<LayoutData>(yaml);

        // バージョンチェック
        ValidateVersion(layoutData.Version);

        // レイアウトを適用
        ApplyLayout(layoutData, editorGraph);
    }

    /// <summary>
    /// EditorGraphからLayoutDataに変換します
    /// </summary>
    private static LayoutData SerializeLayout(EditorGraph editorGraph)
    {
        var layoutData = new LayoutData
        {
            Version = CurrentVersion
        };

        // 各ノードの位置を保存
        foreach (var editorNode in editorGraph.Nodes)
            layoutData.Nodes[editorNode.Node.Id.Value] = new NodePosition
            {
                X = editorNode.X,
                Y = editorNode.Y
            };

        return layoutData;
    }

    /// <summary>
    /// LayoutDataをEditorGraphに適用します
    /// </summary>
    private static void ApplyLayout(LayoutData layoutData, EditorGraph editorGraph)
    {
        // 各ノードの位置を復元
        foreach (var editorNode in editorGraph.Nodes)
            if (layoutData.Nodes.TryGetValue(editorNode.Node.Id.Value, out var position))
            {
                editorNode.X = position.X;
                editorNode.Y = position.Y;
            }
    }

    /// <summary>
    /// バージョンを検証します
    /// </summary>
    private static void ValidateVersion(string version)
    {
        if (!Version.TryParse(version, out var fileVersion) ||
            !Version.TryParse(CurrentVersion, out var currentVersion))
            throw new InvalidOperationException($"Invalid version format: {version}");

        // メジャーバージョンが異なる場合はエラー
        if (fileVersion.Major != currentVersion.Major)
            throw new InvalidOperationException(
                $"Incompatible layout version: {version}. " +
                $"Current version: {CurrentVersion}. " +
                $"Major version mismatch detected.");

        // マイナーバージョンが新しい場合は警告（将来的にはログに出力）
        if (fileVersion.Minor > currentVersion.Minor)
        {
            // TODO: ロギング機能を追加したら警告を出力
            // Console.WriteLine($"Warning: Layout was created with a newer version ({version})");
        }
    }
}