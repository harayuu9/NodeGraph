using NodeGraph.Model;

namespace NodeGraph.App.Undo;

/// <summary>
/// プロパティ変更操作のUndo/Redoアクション
/// </summary>
public class PropertyChangedAction : IUndoableAction
{
    private readonly object? _newValue;
    private readonly Node _node;
    private readonly object? _oldValue;
    private readonly PropertyDescriptor _property;

    public PropertyChangedAction(Node node, PropertyDescriptor property, object? oldValue, object? newValue)
    {
        _node = node;
        _property = property;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public void Execute()
    {
        _property.Setter(_node, _newValue);
    }

    public void Undo()
    {
        _property.Setter(_node, _oldValue);
    }
}