using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.App.Models;
using NodeGraph.App.Selection;
using NodeGraph.App.Undo;
using NodeGraph.App.ViewModels;
using NodeGraph.Model;
using SelectionChangedEventArgs = NodeGraph.App.Selection.SelectionChangedEventArgs;

namespace NodeGraph.App.Controls;

[PseudoClasses(":selected")]
public class NodeControl : ContentControl
{
    public static readonly StyledProperty<EditorNode?> NodeProperty = AvaloniaProperty.Register<NodeControl, EditorNode?>(nameof(Node));

    public static readonly StyledProperty<ExecutionStatus> ExecutionStatusProperty = AvaloniaProperty.Register<NodeControl, ExecutionStatus>(nameof(ExecutionStatus));
    private readonly Dictionary<IPositionable, Point> _selectedNodesStartPositions = [];
    private Point _dragStartPoint;
    private bool _isDragging;

    public NodeControl()
    {
        if (Design.IsDesignMode)
        {
            var n = new FloatConstantNode();
            Node = new EditorNode(new SelectionManager(), n);
        }

        // イベントハンドラの登録
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;

        // コマンドの初期化
        DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
        DuplicateCommand = new RelayCommand(ExecuteDuplicate, CanExecuteDuplicate);
        CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);
        CutCommand = new RelayCommand(ExecuteCut, CanExecuteCut);
        DisconnectAllCommand = new RelayCommand(ExecuteDisconnectAll, CanExecuteDisconnectAll);
        ShowPropertiesCommand = new RelayCommand(ExecuteShowProperties, CanExecuteShowProperties);

        // DataContextを自身に設定（コマンドバインディング用）
        DataContext = this;
    }

    public EditorNode? Node
    {
        get => GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }

    public ExecutionStatus ExecutionStatus
    {
        get => GetValue(ExecutionStatusProperty);
        set => SetValue(ExecutionStatusProperty, value);
    }

    // コマンド
    public ICommand DeleteCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand CutCommand { get; }
    public ICommand DisconnectAllCommand { get; }
    public ICommand ShowPropertiesCommand { get; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NodeProperty) OnNodeChanged(change.GetOldValue<EditorNode?>(), change.GetNewValue<EditorNode?>());
    }

    private void OnNodeChanged(EditorNode? oldNode, EditorNode? newNode)
    {
        if (oldNode != null)
        {
            oldNode.PropertyChanged -= OnNodePropertyChanged;
            oldNode.SelectionManager.SelectionChanged -= OnSelectionChanged;
        }

        if (newNode != null)
        {
            newNode.PropertyChanged += OnNodePropertyChanged;
            newNode.SelectionManager.SelectionChanged += OnSelectionChanged;

            ExecutionStatus = newNode.ExecutionStatus;
            UpdatePseudoClassesFromSelectionManager();
            UpdatePosition(newNode);
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not EditorNode node)
            return;

        if (e.PropertyName is nameof(EditorNode.X) or nameof(EditorNode.Y))
            UpdatePosition(node);
        else if (e.PropertyName == nameof(EditorNode.ExecutionStatus)) ExecutionStatus = node.ExecutionStatus;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdatePseudoClassesFromSelectionManager();
    }

    private void UpdatePseudoClassesFromSelectionManager()
    {
        if (Node == null)
        {
            PseudoClasses.Set(":selected", false);
            return;
        }

        PseudoClasses.Set(":selected", Node.SelectionManager.IsSelected(Node));
    }

    private void UpdatePosition(EditorNode node)
    {
        if (Parent is Canvas)
        {
            Canvas.SetLeft(this, node.X);
            Canvas.SetTop(this, node.Y);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;

        // 左ボタンでドラッグ開始
        if (properties.IsLeftButtonPressed && Node != null && Parent is Visual parent)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(parent);

            // Ctrlキーが押されている場合は複数選択、そうでなければ単一選択
            var isMultiSelect = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            if (isMultiSelect)
            {
                Node.SelectionManager.ToggleSelection(Node);
            }
            else
            {
                // 既に選択されていない場合のみ選択し直す（複数選択を維持するため）
                if (!Node.SelectionManager.IsSelected(Node)) Node.SelectionManager.Select(Node);
            }

            // 選択されている全ノードの開始位置を記録
            _selectedNodesStartPositions.Clear();
            foreach (var selectedItem in Node.SelectionManager.SelectedItems)
                if (selectedItem is IPositionable pos)
                    _selectedNodesStartPositions[pos] = pos.Point();

            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && Node != null && Parent is Visual parent)
        {
            var currentPoint = e.GetPosition(parent);
            var delta = currentPoint - _dragStartPoint;

            // GraphControlの変換を考慮
            if (parent.GetVisualParent() is GraphControl graphControl) delta /= graphControl.Zoom;

            // 選択されている全ノードを同時に移動
            foreach (var (editorNode, startPosition) in _selectedNodesStartPositions)
            {
                editorNode.X = startPosition.X + delta.X;
                editorNode.Y = startPosition.Y + delta.Y;
            }

            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && Node != null)
        {
            var graphControl = FindGraphControl();
            if (graphControl == null)
                return;

            // 移動があった場合のみアクションを登録
            if (_selectedNodesStartPositions.Count > 0)
            {
                // MoveNodesActionに渡す形式に変換
                var nodes = _selectedNodesStartPositions.Keys.OfType<EditorNode>().ToArray();
                var newPositions = nodes.Select(n => new Point(n.X, n.Y)).ToArray();

                // 実際に移動があったかチェック
                var hasMoved = false;
                for (var i = 0; i < nodes.Length; i++)
                {
                    var oldPos = _selectedNodesStartPositions[nodes[i]];
                    if (Math.Abs(nodes[i].X - oldPos.X) > float.Epsilon || Math.Abs(nodes[i].Y - oldPos.Y) > float.Epsilon)
                    {
                        hasMoved = true;
                        break;
                    }
                }

                if (hasMoved)
                {
                    // 一度元に戻してからアクションで再実行
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var oldPos = _selectedNodesStartPositions[nodes[i]];
                        nodes[i].X = oldPos.X;
                        nodes[i].Y = oldPos.Y;
                    }

                    var action = new MoveNodesAction(nodes, newPositions);
                    graphControl.UndoRedoManager!.ExecuteAction(action);

                    if (graphControl.DataContext is MainWindowViewModel viewModel) viewModel.NotifyUndoRedoCanExecuteChanged();
                }
            }

            _isDragging = false;
            _selectedNodesStartPositions.Clear();
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // 削除コマンド
    private bool CanExecuteDelete()
    {
        return Node != null;
    }

    private void ExecuteDelete()
    {
        if (Node == null)
            return;

        var graphControl = FindGraphControl();
        if (graphControl?.Graph == null)
            return;

        // 右クリックされたノードが選択に含まれていない場合は、それだけを選択状態にしてから削除
        if (!Node.SelectionManager.SelectedItems.Contains(Node))
        {
            Node.SelectionManager.ClearSelection();
            Node.SelectionManager.AddToSelection(Node);
        }

        // 共通の削除ロジックを呼び出す
        graphControl.DeleteSelectedItems();
    }

    // 複製コマンド
    private bool CanExecuteDuplicate()
    {
        return Node != null;
    }

    private void ExecuteDuplicate()
    {
        if (Node == null)
            return;

        var graphControl = FindGraphControl();
        if (graphControl == null)
            return;

        // 選択されているノードを複製（このノードを含む）
        var selectedNodes = Node.SelectionManager.SelectedItems
            .OfType<EditorNode>()
            .ToList();

        if (!selectedNodes.Contains(Node))
            selectedNodes.Add(Node);

        // ClipboardServiceを使った複製（Copy→Pasteと同じロジック）
        var duplicatedNodes = graphControl.DuplicateSelectedNodes(selectedNodes);

        // 成功時はUIを更新
        if (duplicatedNodes != null && graphControl.DataContext is MainWindowViewModel viewModel) viewModel.NotifyUndoRedoCanExecuteChanged();
    }

    // コピーコマンド
    private bool CanExecuteCopy()
    {
        return Node != null;
    }

    private void ExecuteCopy()
    {
        if (Node == null)
            return;

        var graphControl = FindGraphControl();
        if (graphControl == null)
            return;

        // GraphControlのCopyメソッドを呼び出すため、KeyDownイベントをシミュレート
        var keyEventArgs = new KeyEventArgs
        {
            RoutedEvent = KeyDownEvent,
            Key = Key.C
        };
        graphControl.RaiseEvent(keyEventArgs);
    }

    // カットコマンド
    private bool CanExecuteCut()
    {
        return Node != null;
    }

    private void ExecuteCut()
    {
        if (Node == null)
            return;

        var graphControl = FindGraphControl();
        if (graphControl == null)
            return;

        // GraphControlのCutメソッドを呼び出すため、KeyDownイベントをシミュレート
        var keyEventArgs = new KeyEventArgs
        {
            RoutedEvent = KeyDownEvent,
            Key = Key.X
        };
        graphControl.RaiseEvent(keyEventArgs);
    }

    // すべての接続を解除コマンド
    private bool CanExecuteDisconnectAll()
    {
        return Node != null;
    }

    private void ExecuteDisconnectAll()
    {
        if (Node?.Node == null)
            return;

        var graphControl = FindGraphControl();
        if (graphControl?.Graph == null)
            return;

        // Undo/Redo対応ですべての接続を解除
        var action = new DisconnectAllAction(graphControl.Graph, Node);
        graphControl.UndoRedoManager!.ExecuteAction(action);

        if (graphControl.DataContext is MainWindowViewModel viewModel) viewModel.NotifyUndoRedoCanExecuteChanged();
    }

    // プロパティ表示コマンド（将来的にプロパティウィンドウを開く）
    private bool CanExecuteShowProperties()
    {
        return Node != null;
    }

    private void ExecuteShowProperties()
    {
        // 現在はプロパティがノード内に表示されているため、何もしない
        // 将来的にはプロパティウィンドウを開く処理を実装
    }

    private GraphControl? FindGraphControl()
    {
        return this.FindAncestorOfType<GraphControl>();
    }
}