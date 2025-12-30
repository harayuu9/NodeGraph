using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using NodeGraph.App.Models;
using NodeGraph.Model;

namespace NodeGraph.App.Controls;

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

    private Ellipse? _portEllipse;

    public PortControl()
    {
        if (Design.IsDesignMode)
        {
            var n = new FloatAddNode();
            Port = new EditorPort("Hoge", n.InputPorts[0]);
        }

        PointerPressed += OnPointerPressed;
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

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _portEllipse = e.NameScope.Find<Ellipse>("PART_PortEllipse");
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Port == null) return;

        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsLeftButtonPressed)
        {
            // ドラッグ開始イベントを発火
            var args = new PortDragEventArgs(PortDragStartedEvent, this, Port, e.GetPosition(this));
            RaiseEvent(args);

            e.Handled = true;
        }
    }

    internal Point? GetCenterIn(Visual relativeTo)
    {
        if (_portEllipse == null)
            return null;

        var ellipseBounds = _portEllipse.Bounds;
        var centerInEllipse = new Point(ellipseBounds.Width / 2, ellipseBounds.Height / 2);
        return _portEllipse.TranslatePoint(centerInEllipse, relativeTo);
    }
}

/// <summary>
/// ポートドラッグイベントの引数
/// </summary>
public class PortDragEventArgs(RoutedEvent routedEvent, PortControl portControl, EditorPort port, Point position) : RoutedEventArgs(routedEvent)
{
    public PortControl PortControl { get; } = portControl;
    public EditorPort Port { get; } = port;
    public Point Position { get; } = position;
}