using System.Collections.Generic;
using System.Linq;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Undo;

/// <summary>
/// ノードの全接続解除操作のUndo/Redoアクション
/// </summary>
public class DisconnectAllAction : IUndoableAction
{
    private readonly EditorGraph _graph;
    private readonly EditorNode _node;
    private readonly List<ConnectionInfo> _removedConnections;

    private record ConnectionInfo(
        EditorNode SourceNode,
        EditorPort SourcePort,
        EditorNode TargetNode,
        EditorPort TargetPort);

    public DisconnectAllAction(EditorGraph graph, EditorNode node)
    {
        _graph = graph;
        _node = node;

        // 削除前に接続情報を保存
        _removedConnections = _graph.Connections
            .Where(c => c.SourceNode == node || c.TargetNode == node)
            .Select(c => new ConnectionInfo(c.SourceNode, c.SourcePort, c.TargetNode, c.TargetPort))
            .ToList();
    }

    public void Execute()
    {
        // 全接続を削除
        var connectionsToRemove = _graph.Connections
            .Where(c => c.SourceNode == _node || c.TargetNode == _node)
            .ToList();

        foreach (var connection in connectionsToRemove)
        {
            // モデルレベルの接続を削除
            var sourcePort = connection.SourcePort.Port as Model.OutputPort;
            var targetPort = connection.TargetPort.Port as Model.InputPort;

            if (sourcePort != null && targetPort != null)
            {
                targetPort.Disconnect(sourcePort);
            }

            // UIレベルの接続を削除
            _graph.Connections.Remove(connection);
        }
    }

    public void Undo()
    {
        // 接続を復元
        foreach (var conn in _removedConnections)
        {
            // モデルレベルの接続を復元
            var sourcePort = conn.SourcePort.Port as Model.OutputPort;
            var targetPort = conn.TargetPort.Port as Model.InputPort;

            if (sourcePort != null && targetPort != null)
            {
                targetPort.Connect(sourcePort);
            }

            // UIレベルの接続を復元
            var editorConnection = new EditorConnection(
                conn.SourceNode,
                conn.SourcePort,
                conn.TargetNode,
                conn.TargetPort);

            _graph.Connections.Add(editorConnection);
        }
    }
}
