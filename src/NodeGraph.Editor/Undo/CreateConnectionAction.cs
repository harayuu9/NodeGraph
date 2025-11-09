using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Undo;

/// <summary>
/// 接続作成操作のUndo/Redoアクション
/// </summary>
public class CreateConnectionAction : IUndoableAction
{
    private readonly EditorGraph _graph;
    private readonly EditorNode _sourceNode;
    private readonly EditorPort _sourcePort;
    private readonly EditorNode _targetNode;
    private readonly EditorPort _targetPort;
    private EditorConnection? _connection;

    public CreateConnectionAction(
        EditorGraph graph,
        EditorNode sourceNode,
        EditorPort sourcePort,
        EditorNode targetNode,
        EditorPort targetPort)
    {
        _graph = graph;
        _sourceNode = sourceNode;
        _sourcePort = sourcePort;
        _targetNode = targetNode;
        _targetPort = targetPort;
    }

    public void Execute()
    {
        // モデルレベルの接続を作成
        var sourcePort = _sourcePort.Port as Model.OutputPort;
        var targetPort = _targetPort.Port as Model.InputPort;

        if (sourcePort != null && targetPort != null)
        {
            targetPort.Connect(sourcePort);
        }

        // UIレベルの接続を作成
        _connection = new EditorConnection(_sourceNode, _sourcePort, _targetNode, _targetPort);
        _graph.Connections.Add(_connection);
    }

    public void Undo()
    {
        if (_connection == null) return;

        // モデルレベルの接続を削除
        var sourcePort = _sourcePort.Port as Model.OutputPort;
        var targetPort = _targetPort.Port as Model.InputPort;

        if (sourcePort != null && targetPort != null)
        {
            targetPort.Disconnect(sourcePort);
        }

        // UIレベルの接続を削除
        _graph.Connections.Remove(_connection);
    }
}
