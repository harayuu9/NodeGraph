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
        if (_connection.SourcePort.Port is Model.OutputPort outputPort && _connection.TargetPort.Port is Model.InputPort inputPort)
        {
            // データポートの接続解除（双方向）
            inputPort.Disconnect(outputPort);
            outputPort.Disconnect(inputPort);
        }
        else if (_connection.SourcePort.Port is Model.ExecOutPort execOutPort && _connection.TargetPort.Port is Model.ExecInPort execInPort)
        {
            // Execポートの接続解除（双方向）
            execOutPort.Disconnect(execInPort);
            execInPort.Disconnect(execOutPort);
        }

        // UIレベルの接続を削除
        _graph.Connections.Remove(_connection);
    }

    public void Undo()
    {
        // モデルレベルの接続を復元
        if (_connection.SourcePort.Port is Model.OutputPort outputPort && _connection.TargetPort.Port is Model.InputPort inputPort)
        {
            // データポートの接続
            inputPort.Connect(outputPort);
        }
        else if (_connection.SourcePort.Port is Model.ExecOutPort execOutPort && _connection.TargetPort.Port is Model.ExecInPort execInPort)
        {
            // Execポートの接続
            execOutPort.Connect(execInPort);
        }

        // UIレベルの接続を復元
        _graph.Connections.Add(_connection);
    }
}
