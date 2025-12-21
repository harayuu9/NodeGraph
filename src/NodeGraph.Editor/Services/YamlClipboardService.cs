using System;
using System.Linq;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Serialization;
using NodeGraph.Model.Serialization;

namespace NodeGraph.Editor.Services;

/// <summary>
/// YAML形式でのクリップボード操作サービス
/// グラフとレイアウトを結合してシリアライズ/デシリアライズします
/// </summary>
public class YamlClipboardService : IClipboardService
{
    private const string GraphMarker = "---GRAPH---";
    private const string LayoutMarker = "---LAYOUT---";

    /// <summary>
    /// ノードをシリアライズしてクリップボード形式の文字列に変換します
    /// </summary>
    public string SerializeNodes(EditorNode[] nodes, EditorGraph graph)
    {
        // 選択されたノードをCloneして、グラフとレイアウトをシリアライズ
        var clonedGraph = graph.Clone(nodes);
        var graphYaml = GraphSerializer.Serialize(clonedGraph.Graph);
        var layoutYaml = EditorLayoutSerializer.SaveLayout(clonedGraph);

        // グラフとレイアウトを結合してクリップボードに保存
        return $"{GraphMarker}\n{graphYaml}\n{LayoutMarker}\n{layoutYaml}";
    }

    /// <summary>
    /// クリップボードの文字列からノードをデシリアライズします
    /// </summary>
    public EditorNode[]? DeserializeNodes(string clipboardData, EditorGraph targetGraph)
    {
        if (string.IsNullOrEmpty(clipboardData)) return null;

        try
        {
            // クリップボードからグラフとレイアウトを分離
            var parts = clipboardData.Split([GraphMarker, LayoutMarker], StringSplitOptions.None);
            if (parts.Length != 3) return null;

            var graphYaml = parts[1];
            var layoutYaml = parts[2];

            // グラフをデシリアライズ
            var pastedGraph = GraphSerializer.Deserialize(graphYaml);

            // EditorNodeに変換
            var editorNodes = pastedGraph.Nodes
                .Select(n => new EditorNode(targetGraph.SelectionManager, n))
                .ToArray();

            // レイアウトを適用（一時的なEditorGraphを作成）
            var tempGraph = new EditorGraph(pastedGraph, targetGraph.SelectionManager);
            foreach (var node in editorNodes) tempGraph.Nodes.Add(node);

            EditorLayoutSerializer.LoadLayout(layoutYaml, tempGraph);

            // 少しオフセットして配置
            foreach (var editorNode in editorNodes)
            {
                editorNode.X += 30;
                editorNode.Y += 30;
            }

            return editorNodes;
        }
        catch
        {
            // デシリアライズ失敗時はnullを返す
            return null;
        }
    }
}