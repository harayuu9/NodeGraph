using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeGraph.Model;

namespace NodeGraph.Editor.Services;

/// <summary>
/// ノードタイプ情報を保持するクラス
/// </summary>
public class NodeTypeInfo
{
    public required Type NodeType { get; init; }
    public required string DisplayName { get; init; }
    public required string Directory { get; init; }

    /// <summary>
    /// ディレクトリと表示名を結合したフルパス（例: "Float/Constant"）
    /// </summary>
    public string FullPath => string.IsNullOrEmpty(Directory) ? DisplayName : $"{Directory}/{DisplayName}";
}

/// <summary>
/// アセンブリから全てのNodeタイプを取得し、階層的に管理するサービス
/// </summary>
public class NodeTypeService
{
    private readonly List<NodeTypeInfo> _nodeTypes = [];

    public IReadOnlyList<NodeTypeInfo> NodeTypes => _nodeTypes;

    public NodeTypeService()
    {
        LoadNodeTypes();
    }

    private void LoadNodeTypes()
    {
        var nodeBaseType = typeof(Node);

        // 全てのロード済みアセンブリからNode派生型を取得
        var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch
                {
                    return [];
                }
            })
            .Where(t => !t.IsAbstract && t.IsClass && nodeBaseType.IsAssignableFrom(t))
            .ToList();

        foreach (var type in nodeTypes)
        {
            // GetDisplayName/GetDirectoryを呼ぶためにインスタンスを作成
            try
            {
                var instance = (Node)Activator.CreateInstance(type)!;
                var displayName = instance.GetDisplayName();
                var directory = instance.GetDirectory();

                _nodeTypes.Add(new NodeTypeInfo
                {
                    NodeType = type,
                    DisplayName = displayName,
                    Directory = directory
                });
            }
            catch
            {
                // インスタンス化に失敗した場合はスキップ
            }
        }

        // ディレクトリ、表示名でソート
        _nodeTypes.Sort((a, b) =>
        {
            var dirComparison = string.Compare(a.Directory, b.Directory, StringComparison.Ordinal);
            return dirComparison != 0 ? dirComparison : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// 検索文字列に基づいてノードタイプをフィルタリング
    /// </summary>
    public IEnumerable<NodeTypeInfo> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return _nodeTypes;

        searchText = searchText.ToLowerInvariant();

        return _nodeTypes.Where(node =>
            node.DisplayName.ToLowerInvariant().Contains(searchText) ||
            node.Directory.ToLowerInvariant().Contains(searchText) ||
            node.FullPath.ToLowerInvariant().Contains(searchText));
    }

    /// <summary>
    /// ディレクトリごとにグループ化されたノードタイプを取得
    /// </summary>
    public IEnumerable<IGrouping<string, NodeTypeInfo>> GetGroupedByDirectory()
    {
        return _nodeTypes.GroupBy(n => n.Directory);
    }
}
