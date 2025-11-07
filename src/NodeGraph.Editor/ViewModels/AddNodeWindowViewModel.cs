using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.Editor.Services;

namespace NodeGraph.Editor.ViewModels;

/// <summary>
/// 階層表示用のノードツリー項目
/// </summary>
public partial class NodeTreeItem : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
    public ObservableCollection<NodeTreeItem> Children { get; } = new();

    /// <summary>
    /// nullの場合はディレクトリ、値がある場合は実際のノード
    /// </summary>
    public NodeTypeInfo? NodeTypeInfo { get; init; }

    /// <summary>
    /// ディレクトリかどうか
    /// </summary>
    public bool IsDirectory => NodeTypeInfo == null;

    /// <summary>
    /// フォントの太さ（ディレクトリは太字）
    /// </summary>
    public FontWeight FontWeight => IsDirectory ? FontWeight.Bold : FontWeight.Normal;
}

/// <summary>
/// AddNodeWindowのViewModel
/// </summary>
public partial class AddNodeWindowViewModel : ViewModelBase
{
    private readonly NodeTypeService _nodeTypeService;

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial NodeTreeItem? SelectedItem { get; set; }
    public ObservableCollection<NodeTreeItem> TreeItems { get; } = [];

    /// <summary>
    /// 選択されたノードタイプ（ウィンドウを閉じる時に設定される）
    /// </summary>
    public NodeTypeInfo? SelectedNodeType { get; private set; }

    public AddNodeWindowViewModel(NodeTypeService nodeTypeService)
    {
        _nodeTypeService = nodeTypeService;
        UpdateTree();
    }

    partial void OnSearchTextChanged(string value)
    {
        UpdateTree();
    }

    private void UpdateTree()
    {
        TreeItems.Clear();

        var filteredNodes = _nodeTypeService.Search(SearchText).ToList();

        // ディレクトリごとにグループ化
        var groupedNodes = filteredNodes.GroupBy(n => n.Directory);

        foreach (var group in groupedNodes)
        {
            var directoryName = string.IsNullOrEmpty(group.Key) ? "Root" : group.Key;

            // ディレクトリノードを作成
            var directoryItem = new NodeTreeItem
            {
                Name = directoryName,
                IsExpanded = true
            };

            // 子ノードを追加
            foreach (var nodeType in group)
            {
                directoryItem.Children.Add(new NodeTreeItem
                {
                    Name = nodeType.DisplayName,
                    NodeTypeInfo = nodeType
                });
            }

            TreeItems.Add(directoryItem);
        }

        // 検索結果が1つだけの場合は自動選択
        if (filteredNodes.Count == 1 && TreeItems.Count == 1 && TreeItems[0].Children.Count == 1)
        {
            SelectedItem = TreeItems[0].Children[0];
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedItem?.NodeTypeInfo != null)
        {
            SelectedNodeType = SelectedItem.NodeTypeInfo;
        }
    }

    /// <summary>
    /// TreeViewで選択されたアイテムのダブルクリックを処理
    /// </summary>
    [RelayCommand]
    private void ItemDoubleClicked(NodeTreeItem? item)
    {
        if (item?.NodeTypeInfo != null)
        {
            SelectedItem = item;
            Confirm();
        }
    }
}
