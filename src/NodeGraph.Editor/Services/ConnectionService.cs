using System.Linq;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Undo;
using NodeGraph.Model;

namespace NodeGraph.Editor.Services;

/// <summary>
/// 接続管理サービスの実装
/// ポート間の接続作成・削除を提供します
/// </summary>
public class ConnectionService : IConnectionService
{
    /// <summary>
    /// 接続を作成し、Undo/Redo可能なアクションで実行します
    /// </summary>
    public bool CreateConnection(
        EditorGraph graph,
        EditorPort sourcePort,
        EditorPort targetPort,
        UndoRedoManager undoRedoManager)
    {
        // Output -> Input の順序を確保
        EditorPort outputPort, inputPort;

        if (sourcePort.IsOutput && targetPort.IsInput)
        {
            outputPort = sourcePort;
            inputPort = targetPort;
        }
        else if (sourcePort.IsInput && targetPort.IsOutput)
        {
            outputPort = targetPort;
            inputPort = sourcePort;
        }
        else
        {
            // 同じ種類のポート同士は接続できない
            return false;
        }

        // EditorNodeを検索（データポートまたはExecポート）
        var outputNode = graph.Nodes.FirstOrDefault(n => n.OutputPorts.Contains(outputPort) || n.ExecOutPorts.Contains(outputPort));
        var inputNode = graph.Nodes.FirstOrDefault(n => n.InputPorts.Contains(inputPort) || n.ExecInPorts.Contains(inputPort));

        if (outputNode == null || inputNode == null)
            return false;

        // ポート間の接続可否をチェック
        if (!CanConnect(outputPort, inputPort))
            return false;

        // 既存の接続があればスキップ
        var existingConnection = graph.Connections.FirstOrDefault(c =>
            c.SourceNode == outputNode && c.SourcePort == outputPort &&
            c.TargetNode == inputNode && c.TargetPort == inputPort);

        if (existingConnection != null)
            return false;

        // SingleConnectPortの場合は既存接続を削除
        DisconnectIfSingleConnect(graph, inputPort, undoRedoManager);
        DisconnectIfSingleConnect(graph, outputPort, undoRedoManager);

        // Undo/Redo対応で接続を作成
        var action = new CreateConnectionAction(graph, outputNode, outputPort, inputNode, inputPort);
        undoRedoManager.ExecuteAction(action);

        return true;
    }

    /// <summary>
    /// 2つのポートが接続可能かどうかを判定します
    /// </summary>
    public bool CanConnect(EditorPort sourcePort, EditorPort targetPort)
    {
        // 双方向チェック（Model層のPort.Connect()と同じロジック）
        return sourcePort.Port.CanConnect(targetPort.Port) && targetPort.Port.CanConnect(sourcePort.Port);
    }

    /// <summary>
    /// 選択された接続を削除します
    /// </summary>
    public void DeleteConnections(
        EditorGraph graph,
        System.Collections.Generic.IEnumerable<EditorConnection> connections,
        UndoRedoManager undoRedoManager)
    {
        foreach (var connection in connections)
        {
            var action = new DeleteConnectionAction(graph, connection);
            undoRedoManager.ExecuteAction(action);
        }
    }

    /// <summary>
    /// SingleConnectPortの場合は既存接続を削除します
    /// </summary>
    private void DisconnectIfSingleConnect(EditorGraph graph, EditorPort port, UndoRedoManager undoRedoManager)
    {
        if (port.Port is SingleConnectPort singleConnectPort)
        {
            var old = singleConnectPort.ConnectedPort;
            if (old != null)
            {
                var node = graph.Nodes.FirstOrDefault(n => n.InputPorts.Contains(port));
                node ??= graph.Nodes.FirstOrDefault(n => n.OutputPorts.Contains(port));
                node ??= graph.Nodes.FirstOrDefault(n => n.ExecInPorts.Contains(port));
                node ??= graph.Nodes.FirstOrDefault(n => n.ExecOutPorts.Contains(port));
                if (node != null)
                {
                    var oldConnection = graph.Connections.FirstOrDefault(c => c.TargetNode == node && c.TargetPort == port);
                    if (oldConnection != null)
                    {
                        // Undo/Redo対応で削除
                        var deleteAction = new DeleteConnectionAction(graph, oldConnection);
                        undoRedoManager.ExecuteAction(deleteAction);
                    }
                }
            }
        }
    }
}
