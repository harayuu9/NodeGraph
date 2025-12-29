using NodeGraph.Editor.Selection;

namespace NodeGraph.UnitTest.Editor;

/// <summary>
/// SelectionManagerのテスト
/// </summary>
public class SelectionManagerTest
{
    /// <summary>
    /// テスト用のISelectable実装
    /// </summary>
    private class MockSelectable : ISelectable
    {
        public MockSelectable(object id)
        {
            SelectionId = id;
        }

        public object SelectionId { get; }
    }

    [Fact]
    public void Select_SingleItem_ClearsExistingSelection()
    {
        var manager = new SelectionManager();
        var item1 = new MockSelectable(1);
        var item2 = new MockSelectable(2);

        manager.Select(item1);
        Assert.Single(manager.SelectedItems);
        Assert.True(manager.IsSelected(item1));

        manager.Select(item2);
        Assert.Single(manager.SelectedItems);
        Assert.False(manager.IsSelected(item1));
        Assert.True(manager.IsSelected(item2));
    }

    [Fact]
    public void AddToSelection_MultipleItems_AllSelected()
    {
        var manager = new SelectionManager();
        var item1 = new MockSelectable(1);
        var item2 = new MockSelectable(2);
        var item3 = new MockSelectable(3);

        manager.AddToSelection(item1);
        manager.AddToSelection(item2);
        manager.AddToSelection(item3);

        Assert.Equal(3, manager.Count);
        Assert.True(manager.IsSelected(item1));
        Assert.True(manager.IsSelected(item2));
        Assert.True(manager.IsSelected(item3));
    }

    [Fact]
    public void AddToSelection_DuplicateItem_NotAdded()
    {
        var manager = new SelectionManager();
        var item = new MockSelectable(1);

        manager.AddToSelection(item);
        manager.AddToSelection(item);

        Assert.Single(manager.SelectedItems);
    }

    [Fact]
    public void RemoveFromSelection_UpdatesSelectedItems()
    {
        var manager = new SelectionManager();
        var item1 = new MockSelectable(1);
        var item2 = new MockSelectable(2);

        manager.AddToSelection(item1);
        manager.AddToSelection(item2);
        Assert.Equal(2, manager.Count);

        manager.RemoveFromSelection(item1);
        Assert.Single(manager.SelectedItems);
        Assert.False(manager.IsSelected(item1));
        Assert.True(manager.IsSelected(item2));
    }

    [Fact]
    public void ToggleSelection_AddsThenRemoves()
    {
        var manager = new SelectionManager();
        var item = new MockSelectable(1);

        // 最初はToggleで追加
        manager.ToggleSelection(item);
        Assert.True(manager.IsSelected(item));
        Assert.Single(manager.SelectedItems);

        // 2回目はToggleで削除
        manager.ToggleSelection(item);
        Assert.False(manager.IsSelected(item));
        Assert.Empty(manager.SelectedItems);
    }

    [Fact]
    public void ClearSelection_EmptiesSelection()
    {
        var manager = new SelectionManager();
        var item1 = new MockSelectable(1);
        var item2 = new MockSelectable(2);

        manager.AddToSelection(item1);
        manager.AddToSelection(item2);
        Assert.Equal(2, manager.Count);

        manager.ClearSelection();
        Assert.Empty(manager.SelectedItems);
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void SelectionChanged_EventFired()
    {
        var manager = new SelectionManager();
        var item = new MockSelectable(1);
        var eventFired = false;
        SelectionChangedEventArgs? eventArgs = null;

        manager.SelectionChanged += (sender, args) =>
        {
            eventFired = true;
            eventArgs = args;
        };

        manager.Select(item);

        Assert.True(eventFired);
        Assert.NotNull(eventArgs);
        Assert.Single(eventArgs.AddedItems);
        Assert.Empty(eventArgs.RemovedItems);
        Assert.Same(item, eventArgs.AddedItems[0]);
    }

    [Fact]
    public void SelectionChanged_ContainsAddedAndRemovedItems()
    {
        var manager = new SelectionManager();
        var item1 = new MockSelectable(1);
        var item2 = new MockSelectable(2);
        SelectionChangedEventArgs? eventArgs = null;

        manager.Select(item1);

        manager.SelectionChanged += (sender, args) => eventArgs = args;
        manager.Select(item2);

        Assert.NotNull(eventArgs);
        Assert.Single(eventArgs.AddedItems);
        Assert.Single(eventArgs.RemovedItems);
        Assert.Same(item2, eventArgs.AddedItems[0]);
        Assert.Same(item1, eventArgs.RemovedItems[0]);
    }

    [Fact]
    public void IsSelected_ReturnsCorrectValue()
    {
        var manager = new SelectionManager();
        var item1 = new MockSelectable(1);
        var item2 = new MockSelectable(2);

        Assert.False(manager.IsSelected(item1));
        Assert.False(manager.IsSelected(item2));

        manager.Select(item1);
        Assert.True(manager.IsSelected(item1));
        Assert.False(manager.IsSelected(item2));
    }

    [Fact]
    public void SelectRange_SelectsMultipleItems()
    {
        var manager = new SelectionManager();
        var items = new[]
        {
            new MockSelectable(1),
            new MockSelectable(2),
            new MockSelectable(3)
        };

        manager.SelectRange(items);

        Assert.Equal(3, manager.Count);
        foreach (var item in items) Assert.True(manager.IsSelected(item));
    }

    [Fact]
    public void SelectRange_ClearsExistingSelection()
    {
        var manager = new SelectionManager();
        var existingItem = new MockSelectable(0);
        var newItems = new[]
        {
            new MockSelectable(1),
            new MockSelectable(2)
        };

        manager.Select(existingItem);
        manager.SelectRange(newItems);

        Assert.Equal(2, manager.Count);
        Assert.False(manager.IsSelected(existingItem));
    }

    [Fact]
    public void Count_ReturnsCorrectValue()
    {
        var manager = new SelectionManager();

        Assert.Equal(0, manager.Count);

        manager.AddToSelection(new MockSelectable(1));
        Assert.Equal(1, manager.Count);

        manager.AddToSelection(new MockSelectable(2));
        Assert.Equal(2, manager.Count);

        manager.ClearSelection();
        Assert.Equal(0, manager.Count);
    }
}
