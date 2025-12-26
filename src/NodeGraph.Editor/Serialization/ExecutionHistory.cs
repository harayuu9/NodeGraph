using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;
using NodeGraph.Model;
using NodeGraph.Model.Serialization;

namespace NodeGraph.Editor.Serialization;

public class ExecutionHistory
{
    public static ExecutionHistory Create(EditorGraph editorGraph)
    {
        var graphYaml = GraphSerializer.Serialize(editorGraph.Graph);
        var layoutYaml = EditorLayoutSerializer.SaveLayout(editorGraph);
        
        return new ExecutionHistory
        {
            _editorGraph = editorGraph,
            GraphYaml = graphYaml,
            LayoutYaml = layoutYaml
        };
    }
    
    private EditorGraph? _editorGraph;
    public string GraphYaml { get; set; } = string.Empty;
    public string LayoutYaml { get; set; } = string.Empty;
    public List<History> Histories { get; set; } = [];
    public DateTime ExecutedAt { get; set; } = DateTime.Now;

    public void Add(Node node)
    {
        if (_editorGraph == null) throw new InvalidOperationException("Cannot add node to execution history without an editor graph");
        if (_editorGraph.Nodes.All(x => x.Node != node)) throw new InvalidOperationException("Cannot find editor node for the given node");
        
        var history = new History
        {
            NodeId = node.Id,
            InputValues = node.InputPorts.Select(x => x.ValueObject).ToArray(),
            OutputValues = node.OutputPorts.Select(x => x.ValueObject).ToArray()
        };
        Histories.Add(history);
    }
    
    public EditorGraph Create(SelectionManager selectionManager)
    {
        var graph = GraphSerializer.Deserialize(GraphYaml);
        var editorGraph = new EditorGraph(graph, selectionManager);
        EditorLayoutSerializer.LoadLayout(LayoutYaml, editorGraph);
        return editorGraph;
    }

    public class History
    {
        public NodeId NodeId { get; set; }
        public object?[] InputValues { get; set; } = [];
        public object?[] OutputValues { get; set; } = [];
    }
}