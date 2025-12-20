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
            DisconnectModel(connection.SourcePort, connection.TargetPort);

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
            ConnectModel(conn.SourcePort, conn.TargetPort);

            // UIレベルの接続を復元
            var editorConnection = new EditorConnection(
                conn.SourceNode,
                conn.SourcePort,
                conn.TargetNode,
                conn.TargetPort);

            _graph.Connections.Add(editorConnection);
        }
    }

    private static void ConnectModel(EditorPort sourcePort, EditorPort targetPort)
    {
        if (sourcePort.Port is Model.OutputPort outputPort && targetPort.Port is Model.InputPort inputPort)
        {
            inputPort.Connect(outputPort);
        }
        else if (sourcePort.Port is Model.ExecOutPort execOutPort && targetPort.Port is Model.ExecInPort execInPort)
        {
            execOutPort.Connect(execInPort);
        }
    }

    private static void DisconnectModel(EditorPort sourcePort, EditorPort targetPort)
    {
        if (sourcePort.Port is Model.OutputPort outputPort && targetPort.Port is Model.InputPort inputPort)
        {
            inputPort.Disconnect(outputPort);
            outputPort.Disconnect(inputPort);
        }
        else if (sourcePort.Port is Model.ExecOutPort execOutPort && targetPort.Port is Model.ExecInPort execInPort)
        {
            execOutPort.Disconnect(execInPort);
            execInPort.Disconnect(execOutPort);
        }
    }
}
