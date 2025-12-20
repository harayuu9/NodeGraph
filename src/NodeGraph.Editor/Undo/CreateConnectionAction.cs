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
        if (_sourcePort.Port is Model.OutputPort outputPort && _targetPort.Port is Model.InputPort inputPort)
        {
            // データポートの接続
            inputPort.Connect(outputPort);
        }
        else if (_sourcePort.Port is Model.ExecOutPort execOutPort && _targetPort.Port is Model.ExecInPort execInPort)
        {
            // Execポートの接続
            execOutPort.Connect(execInPort);
        }

        // UIレベルの接続を作成
        _connection = new EditorConnection(_sourceNode, _sourcePort, _targetNode, _targetPort);
        _graph.Connections.Add(_connection);
    }

    public void Undo()
    {
        if (_connection == null) return;

        // モデルレベルの接続を削除
        if (_sourcePort.Port is Model.OutputPort outputPort && _targetPort.Port is Model.InputPort inputPort)
        {
            // データポートの接続解除（双方向）
            inputPort.Disconnect(outputPort);
            outputPort.Disconnect(inputPort);
        }
        else if (_sourcePort.Port is Model.ExecOutPort execOutPort && _targetPort.Port is Model.ExecInPort execInPort)
        {
            // Execポートの接続解除（双方向）
            execOutPort.Disconnect(execInPort);
            execInPort.Disconnect(execOutPort);
        }

        // UIレベルの接続を削除
        _graph.Connections.Remove(_connection);
    }
}
