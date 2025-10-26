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

    // ドラッグ開始イベント
    public static readonly RoutedEvent<PortDragEventArgs> PortDragStartedEvent =
        RoutedEvent.Register<PortControl, PortDragEventArgs>(nameof(PortDragStarted), RoutingStrategies.Bubble);

    // ドラッグ終了イベント
    public static readonly RoutedEvent<PortDragEventArgs> PortDragCompletedEvent =
        RoutedEvent.Register<PortControl, PortDragEventArgs>(nameof(PortDragCompleted), RoutingStrategies.Bubble);

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

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Port == null) return;

        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(this);

            // ドラッグ開始イベントを発火
            var args = new PortDragEventArgs(PortDragStartedEvent, this, Port, _dragStartPoint);
            RaiseEvent(args);

            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && Port != null)
        {
            // GraphControlでマウス移動を処理するため、ここでは何もしない
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging && Port != null)
        {
            _isDragging = false;
            e.Pointer.Capture(null);

            var releasePoint = e.GetPosition(this);

            // ドラッグ終了イベントを発火
            var args = new PortDragEventArgs(PortDragCompletedEvent, this, Port, releasePoint);
            RaiseEvent(args);

            e.Handled = true;
        }
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
