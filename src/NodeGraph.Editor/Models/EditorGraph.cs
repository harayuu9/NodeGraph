using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorGraph : ObservableObject
{
    private readonly Graph _graph;
    
    public EditorGraph(Graph graph)
    {
        _graph = graph;
        foreach (var graphNode in _graph.Nodes)
        {
            Nodes.Add(new EditorNode(graphNode));
        }
    }
    
    public ObservableCollection<EditorNode> Nodes { get; } = [];
    
    public Graph Graph => _graph;
}