using System.Collections.Generic;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Undo;

/// <summary>
/// ノード複製操作のUndo/Redoアクション
/// </summary>
public class DuplicateNodesAction : IUndoableAction
{
    private readonly EditorGraph _graph;
    private readonly List<EditorNode> _duplicatedNodes;
    private readonly List<EditorConnection> _duplicatedConnections;

    public DuplicateNodesAction(
        EditorGraph graph,
        List<EditorNode> duplicatedNodes,
        List<EditorConnection> duplicatedConnections)
    {
        _graph = graph;
        _duplicatedNodes = duplicatedNodes;
        _duplicatedConnections = duplicatedConnections;
    }

    public void Execute()
    {
        // ノードを追加
        foreach (var node in _duplicatedNodes)
        {
            _graph.Graph.AddNode(node.Node);
            _graph.Nodes.Add(node);
        }

        // 接続を追加
        foreach (var connection in _duplicatedConnections)
        {
            _graph.Connections.Add(connection);
        }
    }

    public void Undo()
    {
        // 接続を削除
        foreach (var connection in _duplicatedConnections)
        {
            var sourcePort = connection.SourcePort.Port as Model.OutputPort;
            var targetPort = connection.TargetPort.Port as Model.InputPort;

            if (sourcePort != null && targetPort != null)
            {
                targetPort.Disconnect(sourcePort);
            }

            _graph.Connections.Remove(connection);
        }

        // ノードを削除
        foreach (var node in _duplicatedNodes)
        {
            _graph.Nodes.Remove(node);
            _graph.Graph.Nodes.Remove(node.Node);
        }
    }

    public List<EditorNode> DuplicatedNodes => _duplicatedNodes;
}
