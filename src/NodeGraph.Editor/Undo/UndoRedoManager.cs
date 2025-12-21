using System.Collections.Generic;

namespace NodeGraph.Editor.Undo;

public class UndoRedoManager
{
    private readonly List<IUndoableAction> _history = [];
    private int _currentIndex = -1;
    private CompositeUndoableAction? _currentTransaction;
    private int _transactionLevel;

    /// <summary>
    /// トランザクションを開始します。
    /// 終了時は必ず EndTransaction() を呼び出してください。
    /// </summary>
    public void BeginTransaction()
    {
        if (_transactionLevel == 0) _currentTransaction = new CompositeUndoableAction();
        _transactionLevel++;
    }

    /// <summary>
    /// トランザクションを終了します。
    /// </summary>
    public void EndTransaction()
    {
        if (_transactionLevel == 0) return;

        _transactionLevel--;
        if (_transactionLevel == 0 && _currentTransaction != null)
        {
            var transaction = _currentTransaction;
            _currentTransaction = null;

            if (transaction.HasActions) AddHistory(transaction);
        }
    }

    public void ExecuteAction(IUndoableAction action)
    {
        if (_currentTransaction != null)
        {
            _currentTransaction.AddAction(action);
            action.Execute();
            return;
        }

        AddHistory(action);
        action.Execute();
    }

    private void AddHistory(IUndoableAction action)
    {
        // _currentIndex以降を削除
        if (_currentIndex + 1 != _history.Count) _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);

        _history.Add(action);
        _currentIndex = _history.Count - 1;
    }

    public bool CanUndo()
    {
        return _currentIndex >= 0 && _transactionLevel == 0;
    }

    public bool CanRedo()
    {
        return _currentIndex < _history.Count - 1 && _transactionLevel == 0;
    }

    public void Undo()
    {
        if (CanUndo())
        {
            _history[_currentIndex].Undo();
            _currentIndex--;
        }
    }

    public void Redo()
    {
        if (CanRedo())
        {
            _currentIndex++;
            _history[_currentIndex].Execute();
        }
    }

    public void Clear()
    {
        _history.Clear();
        _currentIndex = -1;
        _currentTransaction = null;
        _transactionLevel = 0;
    }
}