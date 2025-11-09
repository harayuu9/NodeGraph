using System.Collections.Generic;

namespace NodeGraph.Editor.Undo;

public class UndoRedoManager
{
    private readonly List<IUndoableAction> _history = [];
    private int _currentIndex = -1;
    
    public void ExecuteAction(IUndoableAction action)
    {
        // _currentPoint以降を削除
        if (_currentIndex + 1 != _history.Count)
        {
            _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
        }

        _history.Add(action);
        _currentIndex = _history.Count - 1;
        action.Execute();
    }

    public bool CanUndo() => _currentIndex >= 0;
    public bool CanRedo() => _currentIndex < _history.Count - 1;

    public void Undo()
    {
        if (_currentIndex >= 0)
        {
            _history[_currentIndex].Undo();
            _currentIndex--;
        }
    }
    
    public void Redo()
    {
        if (_currentIndex < _history.Count - 1)
        {
            _currentIndex++;
            _history[_currentIndex].Execute();
        }
    }

    public void Clear()
    {
        _history.Clear();
        _currentIndex = -1;
    }
}