using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using NodeGraph.Editor.Models;
using NodeGraph.Model;

namespace NodeGraph.Editor.Controls;

public enum PortDirection
{
    Left,
    Right
}

public class PortControl : TemplatedControl
{
    public static readonly StyledProperty<EditorPort?> PortProperty = AvaloniaProperty.Register<PortControl, EditorPort?>(nameof(Port));
    public static readonly StyledProperty<PortDirection> DirectionProperty = AvaloniaProperty.Register<PortControl, PortDirection>(nameof(Direction));
    public static readonly StyledProperty<bool> IsHighlightedProperty = AvaloniaProperty.Register<PortControl, bool>(nameof(IsHighlighted));

    public static readonly RoutedEvent<PortDragEventArgs> PortDragStartedEvent = RoutedEvent.Register<PortControl, PortDragEventArgs>(nameof(PortDragStarted), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<PortDragEventArgs> PortDragCompletedEvent = RoutedEvent.Register<PortControl, PortDragEventArgs>(nameof(PortDragCompleted), RoutingStrategies.Bubble);

    public event EventHandler<PortDragEventArgs>? PortDragStarted
    {
        add => AddHandler(PortDragStartedEvent, value);
        remove => RemoveHandler(PortDragStartedEvent, value);
    }

    public event EventHandler<PortDragEventArgs>? PortDragCompleted
    {
        add => AddHandler(PortDragCompletedEvent, value);
        remove => RemoveHandler(PortDragCompletedEvent, value);
    }

    private bool _isDragging;
    private Point _dragStartPoint;

    public PortControl()
    {
        if (Design.IsDesignMode)
        {
            var n = new FloatAddNode();
            Port = EditorPort.FromInput("Hoge", n.InputPorts[0]);
        }

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }

    public EditorPort? Port
    {
        get => GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }

    public PortDirection Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public bool IsHighlighted
    {
        get => GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Port == null) return;

        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);

            // ドラッグ開始イベントを発火
            var args = new PortDragEventArgs(PortDragStartedEvent, this, Port, _dragStartPoint);
            RaiseEvent(args);

            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        // GraphControlでマウス移動を処理
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && Port != null)
        {
            _isDragging = false;

            // ドラッグ終了イベントは発火しない（GraphControlで処理）
            e.Handled = true;
        }
    }

    internal void CompleteDrag()
    {
        _isDragging = false;
    }
}

/// <summary>
/// ポートドラッグイベントの引数
/// </summary>
public class PortDragEventArgs : RoutedEventArgs
{
    public PortControl PortControl { get; }
    public EditorPort Port { get; }
    public Point Position { get; }

    public PortDragEventArgs(RoutedEvent routedEvent, PortControl portControl, EditorPort port, Point position)
        : base(routedEvent)
    {
        PortControl = portControl;
        Port = port;
        Position = position;
    }
}
