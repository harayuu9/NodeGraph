using System.Collections.Generic;
using System.Linq;
using NodeGraph.App.Models;
using NodeGraph.Model;

namespace NodeGraph.App.Undo;

/// <summary>
/// ノード削除操作のUndo/Redoアクション
/// </summary>
public class DeleteNodeAction : IUndoableAction
{
    private readonly List<ConnectionInfo> _connections;
    private readonly EditorGraph _graph;
    private readonly EditorNode _node;

    public DeleteNodeAction(EditorGraph graph, EditorNode node)
    {
        _graph = graph;
        _node = node;

        // 削除前に接続情報を保存
        _connections = _graph.Connections
            .Where(c => c.SourceNode == node || c.TargetNode == node)
            .Select(c => new ConnectionInfo(c.SourceNode, c.SourcePort, c.TargetNode, c.TargetPort))
            .ToList();
    }

    public void Execute()
    {
        // ノードを削除（接続も自動的に削除される）
        _graph.RemoveNode(_node);
    }

    public void Undo()
    {
        // ノードを復元
        _graph.Graph.AddNode(_node.Node);
        _graph.Nodes.Add(_node);

        // 接続を復元
        foreach (var conn in _connections)
        {
            // モデルレベルの接続を復元
            var sourcePort = conn.SourcePort.Port as OutputPort;
            var targetPort = conn.TargetPort.Port as InputPort;

            if (sourcePort != null && targetPort != null) targetPort.Connect(sourcePort);

            // UI レベルの接続を復元
            var editorConnection = new EditorConnection(
                conn.SourceNode,
                conn.SourcePort,
                conn.TargetNode,
                conn.TargetPort);

            _graph.Connections.Add(editorConnection);
        }
    }

    private record ConnectionInfo(
        EditorNode SourceNode,
        EditorPort SourcePort,
        EditorNode TargetNode,
        EditorPort TargetPort);
}