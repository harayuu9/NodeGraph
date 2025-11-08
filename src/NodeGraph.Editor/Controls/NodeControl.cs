using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;
using NodeGraph.Model;

namespace NodeGraph.Editor.Controls;

[PseudoClasses(":selected")]
public class NodeControl : ContentControl
{
    private bool _isDragging;
    private Point _dragStartPoint;
    private readonly Dictionary<IPositionable, Point> _selectedNodesStartPositions = [];

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

    public static readonly StyledProperty<EditorNode?> NodeProperty = AvaloniaProperty.Register<NodeControl, EditorNode?>(nameof(Node));

    public EditorNode? Node
    {
        get => GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }
    
    public static readonly StyledProperty<ExecutionStatus> ExecutionStatusProperty = AvaloniaProperty.Register<NodeControl, ExecutionStatus>(nameof(ExecutionStatus));

    public ExecutionStatus ExecutionStatus
    {
        get => GetValue(ExecutionStatusProperty);
        set => SetValue(ExecutionStatusProperty, value);
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NodeProperty)
        {
            OnNodeChanged(change.GetOldValue<EditorNode?>(), change.GetNewValue<EditorNode?>());
        }
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

    private void OnNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not EditorNode node)
            return;

        if (e.PropertyName is nameof(EditorNode.X) or nameof(EditorNode.Y))
        {
            UpdatePosition(node);
        }
        else if (e.PropertyName == nameof(EditorNode.ExecutionStatus))
        {
            ExecutionStatus = node.ExecutionStatus;
        }
    }

    private void OnSelectionChanged(object? sender, Selection.SelectionChangedEventArgs e)
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
        if (Parent is Canvas canvas)
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
                if (!Node.SelectionManager.IsSelected(Node))
                {
                    Node.SelectionManager.Select(Node);
                }
            }

            // 選択されている全ノードの開始位置を記録
            _selectedNodesStartPositions.Clear();
            foreach (var selectedItem in Node.SelectionManager.SelectedItems)
            {
                if (selectedItem is IPositionable pos)
                {
                    _selectedNodesStartPositions[pos] = pos.Point();
                }
            }

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
            if (parent.GetVisualParent() is GraphControl graphControl)
            {
                delta /= graphControl.Zoom;
            }

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
        if (_isDragging)
        {
            _isDragging = false;
            _selectedNodesStartPositions.Clear();
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // コマンド
    public ICommand DeleteCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand CutCommand { get; }
    public ICommand DisconnectAllCommand { get; }
    public ICommand ShowPropertiesCommand { get; }

    // 削除コマンド
    private bool CanExecuteDelete() => Node != null;

    private void ExecuteDelete()
    {
        if (Node == null)
            return;

        var graphControl = this.FindAncestorOfType<GraphControl>();
        if (graphControl?.Graph == null)
            return;

        // 選択されているノードを削除（このノードを含む）
        var selectedNodes = Node.SelectionManager.SelectedItems
            .OfType<EditorNode>()
            .ToList();

        if (!selectedNodes.Contains(Node))
            selectedNodes.Add(Node);

        foreach (var editorNode in selectedNodes)
        {
            graphControl.Graph.RemoveNode(editorNode);
        }

        Node.SelectionManager.ClearSelection();
    }

    // 複製コマンド
    private bool CanExecuteDuplicate() => Node != null;

    private void ExecuteDuplicate()
    {
        if (Node == null)
            return;

        var graphControl = this.FindAncestorOfType<GraphControl>();
        if (graphControl?.Graph == null)
            return;

        // 選択されているノードを複製（このノードを含む）
        var selectedNodes = Node.SelectionManager.SelectedItems
            .OfType<EditorNode>()
            .ToList();

        if (!selectedNodes.Contains(Node))
            selectedNodes.Add(Node);

        // GraphControlの共通メソッドを使用して複製（接続も含む）
        var newNodes = graphControl.DuplicateNodesWithConnections(selectedNodes);

        // 複製されたノードを選択
        Node.SelectionManager.ClearSelection();
        foreach (var newNode in newNodes)
        {
            Node.SelectionManager.Select(newNode);
        }
    }

    // コピーコマンド
    private bool CanExecuteCopy() => Node != null;

    private void ExecuteCopy()
    {
        if (Node == null)
            return;

        var graphControl = this.FindAncestorOfType<GraphControl>();
        if (graphControl == null)
            return;

        // GraphControlのCopyメソッドを呼び出すため、KeyDownイベントをシミュレート
        var keyEventArgs = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.C
        };
        graphControl.RaiseEvent(keyEventArgs);
    }

    // カットコマンド
    private bool CanExecuteCut() => Node != null;

    private void ExecuteCut()
    {
        if (Node == null)
            return;

        var graphControl = this.FindAncestorOfType<GraphControl>();
        if (graphControl == null)
            return;

        // GraphControlのCutメソッドを呼び出すため、KeyDownイベントをシミュレート
        var keyEventArgs = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.X
        };
        graphControl.RaiseEvent(keyEventArgs);
    }

    // すべての接続を解除コマンド
    private bool CanExecuteDisconnectAll() => Node != null;

    private void ExecuteDisconnectAll()
    {
        if (Node?.Node == null)
            return;

        // すべての入力ポートの接続を解除
        foreach (var port in Node.Node.InputPorts)
        {
            port.DisconnectAll();
        }

        // すべての出力ポートの接続を解除
        foreach (var port in Node.Node.OutputPorts)
        {
            port.DisconnectAll();
        }
    }

    // プロパティ表示コマンド（将来的にプロパティウィンドウを開く）
    private bool CanExecuteShowProperties() => Node != null;

    private void ExecuteShowProperties()
    {
        // 現在はプロパティがノード内に表示されているため、何もしない
        // 将来的にはプロパティウィンドウを開く処理を実装
    }
}