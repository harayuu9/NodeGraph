using System;
using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// Port間を繋ぐコネクタコントロール
/// </summary>
public class ConnectorControl : Path
{
    public static readonly StyledProperty<EditorConnection?> ConnectionProperty = AvaloniaProperty.Register<ConnectorControl, EditorConnection?>(nameof(Connection));
    public static readonly StyledProperty<double> StartXProperty = AvaloniaProperty.Register<ConnectorControl, double>(nameof(StartX));
    public static readonly StyledProperty<double> StartYProperty = AvaloniaProperty.Register<ConnectorControl, double>(nameof(StartY));
    public static readonly StyledProperty<double> EndXProperty = AvaloniaProperty.Register<ConnectorControl, double>(nameof(EndX));
    public static readonly StyledProperty<double> EndYProperty = AvaloniaProperty.Register<ConnectorControl, double>(nameof(EndY));

    public EditorConnection? Connection
    {
        get => GetValue(ConnectionProperty);
        set => SetValue(ConnectionProperty, value);
    }

    public double StartX
    {
        get => GetValue(StartXProperty);
        set => SetValue(StartXProperty, value);
    }

    public double StartY
    {
        get => GetValue(StartYProperty);
        set => SetValue(StartYProperty, value);
    }

    public double EndX
    {
        get => GetValue(EndXProperty);
        set => SetValue(EndXProperty, value);
    }

    public double EndY
    {
        get => GetValue(EndYProperty);
        set => SetValue(EndYProperty, value);
    }

    static ConnectorControl()
    {
        AffectsRender<ConnectorControl>(StartXProperty, StartYProperty, EndXProperty, EndYProperty);
        StartXProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdatePath());
        StartYProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdatePath());
        EndXProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdatePath());
        EndYProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdatePath());
    }

    public ConnectorControl()
    {
        Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));
        StrokeThickness = 2;
        IsHitTestVisible = false;
    }

    private void UpdatePath()
    {
        var startX = StartX;
        var startY = StartY;
        var endX = EndX;
        var endY = EndY;

        // ベジェ曲線の制御点を計算
        var distance = Math.Abs(endX - startX);
        var controlPointOffset = Math.Min(distance * 0.5, 100);

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure
        {
            StartPoint = new Point(startX, startY),
            IsClosed = false
        };

        // 3次ベジェ曲線を使用して滑らかな接続線を描画
        var bezierSegment = new BezierSegment
        {
            Point1 = new Point(startX + controlPointOffset, startY),
            Point2 = new Point(endX - controlPointOffset, endY),
            Point3 = new Point(endX, endY)
        };

        pathFigure.Segments?.Add(bezierSegment);
        pathGeometry.Figures?.Add(pathFigure);

        Data = pathGeometry;
    }
}
