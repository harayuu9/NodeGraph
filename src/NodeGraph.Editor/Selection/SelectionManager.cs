using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeGraph.Editor.Selection;

/// <summary>
/// 選択状態を一元管理するクラス
/// </summary>
public class SelectionManager
{
    private readonly HashSet<object> _selectedIds = [];
    private readonly List<ISelectable> _selectedItems = [];

    /// <summary>
    /// 現在選択されているアイテムの読み取り専用コレクション
    /// </summary>
    public IReadOnlyList<ISelectable> SelectedItems => _selectedItems.AsReadOnly();

    /// <summary>
    /// 選択されているアイテムの数
    /// </summary>
    public int Count => _selectedItems.Count;

    /// <summary>
    /// 選択が変更されたときに発生するイベント
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// 指定されたアイテムが選択されているかどうかを判定します
    /// </summary>
    public bool IsSelected(ISelectable item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        return _selectedIds.Contains(item.SelectionId);
    }

    /// <summary>
    /// 単一のアイテムを選択します（既存の選択をクリア）
    /// </summary>
    public void Select(ISelectable item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var previouslySelected = _selectedItems.ToList();
        var isAlreadySelected = _selectedIds.Contains(item.SelectionId);

        // 既に単一選択されている場合は何もしない
        if (isAlreadySelected && _selectedItems.Count == 1)
            return;

        _selectedIds.Clear();
        _selectedItems.Clear();
        _selectedIds.Add(item.SelectionId);
        _selectedItems.Add(item);

        OnSelectionChanged(previouslySelected, _selectedItems);
    }

    /// <summary>
    /// アイテムを選択に追加します（既存の選択を維持）
    /// </summary>
    public void AddToSelection(ISelectable item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (_selectedIds.Contains(item.SelectionId))
            return;

        var previouslySelected = _selectedItems.ToList();

        _selectedIds.Add(item.SelectionId);
        _selectedItems.Add(item);

        OnSelectionChanged(previouslySelected, _selectedItems);
    }

    /// <summary>
    /// アイテムを選択から削除します
    /// </summary>
    public void RemoveFromSelection(ISelectable item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (!_selectedIds.Contains(item.SelectionId))
            return;

        var previouslySelected = _selectedItems.ToList();

        _selectedIds.Remove(item.SelectionId);
        _selectedItems.RemoveAll(i => Equals(i.SelectionId, item.SelectionId));

        OnSelectionChanged(previouslySelected, _selectedItems);
    }

    /// <summary>
    /// アイテムの選択状態をトグルします（選択されていれば解除、解除されていれば追加）
    /// </summary>
    public void ToggleSelection(ISelectable item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (_selectedIds.Contains(item.SelectionId))
            RemoveFromSelection(item);
        else
            AddToSelection(item);
    }

    /// <summary>
    /// 複数のアイテムを選択します（既存の選択をクリア）
    /// </summary>
    public void SelectRange(IEnumerable<ISelectable> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var previouslySelected = _selectedItems.ToList();

        _selectedIds.Clear();
        _selectedItems.Clear();

        foreach (var item in items)
            if (!_selectedIds.Contains(item.SelectionId))
            {
                _selectedIds.Add(item.SelectionId);
                _selectedItems.Add(item);
            }

        OnSelectionChanged(previouslySelected, _selectedItems);
    }

    /// <summary>
    /// すべての選択をクリアします
    /// </summary>
    public void ClearSelection()
    {
        if (_selectedItems.Count == 0)
            return;

        var previouslySelected = _selectedItems.ToList();

        _selectedIds.Clear();
        _selectedItems.Clear();

        OnSelectionChanged(previouslySelected, _selectedItems);
    }

    private void OnSelectionChanged(IReadOnlyList<ISelectable> previousSelection, IReadOnlyList<ISelectable> currentSelection)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(previousSelection, currentSelection));
    }
}

/// <summary>
/// 選択変更イベントの引数
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    public SelectionChangedEventArgs(IReadOnlyList<ISelectable> previousSelection, IReadOnlyList<ISelectable> currentSelection)
    {
        PreviousSelection = previousSelection;
        CurrentSelection = currentSelection;
        AddedItems = currentSelection.Except(previousSelection).ToList();
        RemovedItems = previousSelection.Except(currentSelection).ToList();
    }

    /// <summary>
    /// 変更前の選択
    /// </summary>
    public IReadOnlyList<ISelectable> PreviousSelection { get; }

    /// <summary>
    /// 変更後の選択
    /// </summary>
    public IReadOnlyList<ISelectable> CurrentSelection { get; }

    /// <summary>
    /// 新たに追加されたアイテム
    /// </summary>
    public IReadOnlyList<ISelectable> AddedItems { get; }

    /// <summary>
    /// 削除されたアイテム
    /// </summary>
    public IReadOnlyList<ISelectable> RemovedItems { get; }
}