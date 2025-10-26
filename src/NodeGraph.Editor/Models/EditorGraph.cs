using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Editor.Selection;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorGraph : ObservableObject
{
    private readonly Graph _graph;

    public EditorGraph(Graph graph, SelectionManager selectionManager)
    {
        _graph = graph;
        SelectionManager = selectionManager;
        foreach (var graphNode in _graph.Nodes)
        {
            Nodes.Add(new EditorNode(selectionManager, graphNode));
        }
    }

    public SelectionManager SelectionManager { get; }

    public ObservableCollection<EditorNode> Nodes { get; } = [];

    public Graph Graph => _graph;
}