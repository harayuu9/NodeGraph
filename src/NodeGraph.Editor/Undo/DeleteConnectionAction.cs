using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Undo;

/// <summary>
/// 接続削除操作のUndo/Redoアクション
/// </summary>
public class DeleteConnectionAction : IUndoableAction
{
    private readonly EditorGraph _graph;
    private readonly EditorConnection _connection;

    public DeleteConnectionAction(EditorGraph graph, EditorConnection connection)
    {
        _graph = graph;
        _connection = connection;
    }

    public void Execute()
    {
        // モデルレベルの接続を削除
        var sourcePort = _connection.SourcePort.Port as Model.OutputPort;
        var targetPort = _connection.TargetPort.Port as Model.InputPort;

        if (sourcePort != null && targetPort != null)
        {
            targetPort.Disconnect(sourcePort);
        }

        // UIレベルの接続を削除
        _graph.Connections.Remove(_connection);
    }

    public void Undo()
    {
        // モデルレベルの接続を復元
        var sourcePort = _connection.SourcePort.Port as Model.OutputPort;
        var targetPort = _connection.TargetPort.Port as Model.InputPort;

        if (sourcePort != null && targetPort != null)
        {
            targetPort.Connect(sourcePort);
        }

        // UIレベルの接続を復元
        _graph.Connections.Add(_connection);
    }
}
