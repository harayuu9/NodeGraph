using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.VisualTree;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Controls;

[PseudoClasses(":selected")]
public class NodeControl : ContentControl
{
    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _nodeStartPosition;

    public NodeControl()
    {
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

        if (e.PropertyName is nameof(EditorNode.PositionX) or nameof(EditorNode.PositionY))
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
            Canvas.SetLeft(this, node.PositionX);
            Canvas.SetTop(this, node.PositionY);
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
            _nodeStartPosition = new Point(Node.PositionX, Node.PositionY);

            // Ctrlキーが押されている場合は複数選択、そうでなければ単一選択
            var isMultiSelect = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            if (isMultiSelect)
            {
                Node.SelectionManager.ToggleSelection(Node);
            }
            else
            {
                Node.SelectionManager.Select(Node);
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

            Node.PositionX = _nodeStartPosition.X + delta.X;
            Node.PositionY = _nodeStartPosition.Y + delta.Y;

            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }
}