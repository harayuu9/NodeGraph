using NodeGraph.App.Undo;

namespace NodeGraph.UnitTest;

public class UndoRedoManagerTest
{
    [Fact]
    public void Transaction_GroupsActions()
    {
        var manager = new UndoRedoManager();
        var action1 = new MockAction();
        var action2 = new MockAction();

        manager.BeginTransaction();
        manager.ExecuteAction(action1);
        manager.ExecuteAction(action2);
        manager.EndTransaction();

        Assert.Equal(1, action1.ExecuteCount);
        Assert.Equal(1, action2.ExecuteCount);
        Assert.True(manager.CanUndo());

        manager.Undo();

        Assert.Equal(1, action1.UndoCount);
        Assert.Equal(1, action2.UndoCount);
        Assert.False(manager.CanUndo());
        Assert.True(manager.CanRedo());

        manager.Redo();

        Assert.Equal(2, action1.ExecuteCount);
        Assert.Equal(2, action2.ExecuteCount);
    }

    [Fact]
    public void NestedTransaction_Works()
    {
        var manager = new UndoRedoManager();
        var action1 = new MockAction();
        var action2 = new MockAction();

        manager.BeginTransaction();
        manager.ExecuteAction(action1);

        manager.BeginTransaction();
        manager.ExecuteAction(action2);
        manager.EndTransaction();

        manager.EndTransaction();

        Assert.True(manager.CanUndo());
        manager.Undo();

        Assert.Equal(1, action1.UndoCount);
        Assert.Equal(1, action2.UndoCount);
        Assert.False(manager.CanUndo());
    }

    [Fact]
    public void EmptyTransaction_DoesNotAddHistory()
    {
        var manager = new UndoRedoManager();

        manager.BeginTransaction();
        manager.EndTransaction();

        Assert.False(manager.CanUndo());
    }

    [Fact]
    public void DeleteSelectedItems_Mock_GroupsMultipleActions()
    {
        var manager = new UndoRedoManager();
        var actions = new List<MockAction>();

        // DeleteSelectedItems の簡易再現
        void DeleteSelectedItemsMock(int nodeCount)
        {
            manager.BeginTransaction();
            try
            {
                for (var i = 0; i < nodeCount; i++)
                {
                    var action = new MockAction();
                    actions.Add(action);
                    manager.ExecuteAction(action);
                }
            }
            finally
            {
                manager.EndTransaction();
            }
        }

        DeleteSelectedItemsMock(3);

        Assert.True(manager.CanUndo());
        Assert.Equal(1, actions[0].ExecuteCount);
        Assert.Equal(1, actions[1].ExecuteCount);
        Assert.Equal(1, actions[2].ExecuteCount);

        manager.Undo();

        Assert.Equal(1, actions[0].UndoCount);
        Assert.Equal(1, actions[1].UndoCount);
        Assert.Equal(1, actions[2].UndoCount);
        Assert.False(manager.CanUndo()); // 3つのアクションが1つにまとまっているはず
    }

    private class MockAction : IUndoableAction
    {
        public int ExecuteCount { get; private set; }
        public int UndoCount { get; private set; }

        public void Execute()
        {
            ExecuteCount++;
        }

        public void Undo()
        {
            UndoCount++;
        }
    }
}