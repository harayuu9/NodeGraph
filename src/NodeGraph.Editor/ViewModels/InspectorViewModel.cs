using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;

namespace NodeGraph.Editor.ViewModels;

/// <summary>
/// Inspectorパネルのビューモデル
/// </summary>
public partial class InspectorViewModel : ObservableObject
{
    private readonly SelectionManager _selectionManager;

    public InspectorViewModel(SelectionManager selectionManager)
    {
        _selectionManager = selectionManager;
        _selectionManager.SelectionChanged += OnSelectionChanged;
    }

    /// <summary>
    /// 選択中のノード
    /// </summary>
    [ObservableProperty]
    public partial EditorNode? SelectedNode { get; private set; }

    /// <summary>
    /// Inspectorに表示するプロパティ
    /// </summary>
    public ObservableCollection<PropertyViewModel> Properties { get; } = [];

    /// <summary>
    /// 単一のノードが選択されているかどうか
    /// </summary>
    public bool HasSelection => SelectedNode != null;

    /// <summary>
    /// 選択中のノード名
    /// </summary>
    public string NodeName => SelectedNode?.Name ?? "No Selection";

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // 単一のEditorNodeが選択されている場合のみ表示
        var selectedNodes = _selectionManager.SelectedItems.OfType<EditorNode>().ToList();
        var selectedNode = selectedNodes.Count == 1 ? selectedNodes[0] : null;

        UpdateSelectedNode(selectedNode);
    }

    private void UpdateSelectedNode(EditorNode? node)
    {
        SelectedNode = node;
        Properties.Clear();

        if (node != null)
            foreach (var prop in node.InspectorProperties)
                Properties.Add(prop);

        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(NodeName));
    }
}
