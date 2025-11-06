using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.VisualTree;
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
    }

    public static readonly StyledProperty<EditorNode?> NodeProperty = AvaloniaProperty.Register<NodeControl, EditorNode?>(nameof(Node));

    public EditorNode? Node
    {
        get => GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }
    
    public static readonly StyledProperty<ExecutionStatus> ExecutionStatusProperty = AvaloniaProperty.Register<NodeControl, ExecutionStatus>(nameof(ExecutionStatus));
    
    public ExecutionStatus ExecutionStatus => Node?.ExecutionStatus ?? ExecutionStatus.None;
    
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
}