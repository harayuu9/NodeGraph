using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using NodeGraph.App.Converters;
using NodeGraph.App.Models;

namespace NodeGraph.App.Controls;

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
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<ConnectorControl, bool>(nameof(IsSelected));
    private static readonly TypeToColorConverter _typeToColorConverter = new();

    private IBrush? _selectedStrokeBrush;

    static ConnectorControl()
    {
        AffectsRender<ConnectorControl>(StartXProperty, StartYProperty, EndXProperty, EndYProperty, IsSelectedProperty);
        StartXProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdatePath());
        StartYProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdatePath());
        EndXProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdatePath());
        EndYProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdatePath());
        IsSelectedProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdateAppearance());
        ConnectionProperty.Changed.AddClassHandler<ConnectorControl>((control, _) => control.UpdateAppearance());
    }

    public ConnectorControl()
    {
        // デフォルト値を設定（リソースが利用できない場合のフォールバック）
        Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));
        StrokeThickness = 2;
        IsHitTestVisible = true;
        Cursor = new Cursor(StandardCursorType.Hand);

        PointerPressed += OnPointerPressed;
    }

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

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // テーマリソースから色を取得
        _selectedStrokeBrush = this.FindResource("ConnectorSelectedStrokeBrush") as IBrush;

        // 現在の状態に応じて外観を更新
        UpdateAppearance();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Connection?.SourceNode.SelectionManager == null)
            return;

        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsLeftButtonPressed)
        {
            var selectionManager = Connection.SourceNode.SelectionManager;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                // Ctrlキー + クリックで選択トグル
                selectionManager.ToggleSelection(Connection);
            else
                // 通常のクリックで単一選択
                selectionManager.Select(Connection);

            e.Handled = true;
        }
    }

    private void UpdateAppearance()
    {
        if (IsSelected)
        {
            // 選択時は専用の色を使用
            Stroke = _selectedStrokeBrush ?? new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
            StrokeThickness = 3;
        }
        else
        {
            // 通常時は型に応じた色を使用
            // SourcePortとTargetPortの両方から色を取得してグラデーションを作成
            var sourceTypeName = Connection?.SourcePort.TypeName;
            var targetTypeName = Connection?.TargetPort.TypeName;

            var sourceBrush = _typeToColorConverter.Convert(sourceTypeName, typeof(IBrush), null, CultureInfo.InvariantCulture) as SolidColorBrush;
            var targetBrush = _typeToColorConverter.Convert(targetTypeName, typeof(IBrush), null, CultureInfo.InvariantCulture) as SolidColorBrush;

            var sourceColor = sourceBrush?.Color ?? Color.FromRgb(0x00, 0x7A, 0xCC);
            var targetColor = targetBrush?.Color ?? Color.FromRgb(0x00, 0x7A, 0xCC);

            // 両方の色が同じ場合は単色、異なる場合はグラデーション
            if (sourceColor == targetColor)
            {
                Stroke = new SolidColorBrush(sourceColor);
            }
            else
            {
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative)
                };
                gradient.GradientStops.Add(new GradientStop(sourceColor, 0));
                gradient.GradientStops.Add(new GradientStop(targetColor, 1));
                Stroke = gradient;
            }

            StrokeThickness = 2;
        }
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

    /// <summary>
    /// 指定された矩形とこのコネクタが交差するかどうかを判定します
    /// </summary>
    public bool Intersects(Rect rect)
    {
        var p0 = new Point(StartX, StartY);
        var distance = Math.Abs(EndX - StartX);
        var offset = Math.Min(distance * 0.5, 100);
        var p1 = new Point(StartX + offset, StartY);
        var p2 = new Point(EndX - offset, EndY);
        var p3 = new Point(EndX, EndY);

        // 簡易的な交差判定：曲線をN分割して線分として判定
        const int subdivisions = 10;
        var prevPoint = p0;
        for (var i = 1; i <= subdivisions; i++)
        {
            var t = (double)i / subdivisions;
            var currentPoint = CalculateBezier(t, p0, p1, p2, p3);
            if (LineIntersectsRect(prevPoint, currentPoint, rect)) return true;
            prevPoint = currentPoint;
        }

        return false;
    }

    private static Point CalculateBezier(double t, Point p0, Point p1, Point p2, Point p3)
    {
        var u = 1 - t;
        var tt = t * t;
        var uu = u * u;
        var uuu = uu * u;
        var ttt = tt * t;

        var x = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
        var y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;

        return new Point(x, y);
    }

    private static bool LineIntersectsRect(Point p1, Point p2, Rect rect)
    {
        // 線分の両端のいずれかが矩形内にある
        if (rect.Contains(p1) || rect.Contains(p2)) return true;

        // 線分が矩形のいずれかの辺と交差するか
        // Line-Rectangle intersection
        return LineIntersectsLine(p1, p2, rect.TopLeft, rect.TopRight) ||
               LineIntersectsLine(p1, p2, rect.TopRight, rect.BottomRight) ||
               LineIntersectsLine(p1, p2, rect.BottomRight, rect.BottomLeft) ||
               LineIntersectsLine(p1, p2, rect.BottomLeft, rect.TopLeft);
    }

    private static bool LineIntersectsLine(Point a1, Point a2, Point b1, Point b2)
    {
        var d = (a2.X - a1.X) * (b2.Y - b1.Y) - (a2.Y - a1.Y) * (b2.X - b1.X);
        if (d == 0) return false;

        var u = ((b1.X - a1.X) * (b2.Y - b1.Y) - (b1.Y - a1.Y) * (b2.X - b1.X)) / d;
        var v = ((b1.X - a1.X) * (a2.Y - a1.Y) - (b1.Y - a1.Y) * (a2.X - a1.X)) / d;

        return u >= 0 && u <= 1 && v >= 0 && v <= 1;
    }
}