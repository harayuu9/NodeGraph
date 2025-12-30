using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace NodeGraph.App.Controls;

/// <summary>
/// 軽量なグリッド描画用デコレーター。
/// コンテンツにはトランスフォームを適用せず、
/// Pan/Zoom 値からスクリーン座標で効率的に描画します。
/// </summary>
public class GridDecorator : Decorator
{
    public static readonly StyledProperty<bool> EnableGridProperty = AvaloniaProperty.Register<GridDecorator, bool>(nameof(EnableGrid), true);
    public static readonly StyledProperty<double> CellSizeProperty = AvaloniaProperty.Register<GridDecorator, double>(nameof(CellSize), 20.0);
    public static readonly StyledProperty<double> ZoomProperty = AvaloniaProperty.Register<GridDecorator, double>(nameof(Zoom), 1.0);
    public static readonly StyledProperty<Vector> PanOffsetProperty = AvaloniaProperty.Register<GridDecorator, Vector>(nameof(PanOffset));
    public static readonly StyledProperty<IBrush?> StrokeProperty = AvaloniaProperty.Register<GridDecorator, IBrush?>(nameof(Stroke), new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)));

    public bool EnableGrid
    {
        get => GetValue(EnableGridProperty);
        set => SetValue(EnableGridProperty, value);
    }

    public double CellSize
    {
        get => GetValue(CellSizeProperty);
        set => SetValue(CellSizeProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public Vector PanOffset
    {
        get => GetValue(PanOffsetProperty);
        set => SetValue(PanOffsetProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == EnableGridProperty ||
            change.Property == CellSizeProperty ||
            change.Property == ZoomProperty ||
            change.Property == PanOffsetProperty ||
            change.Property == StrokeProperty)
            InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!EnableGrid)
            return;

        var size = CellSize;
        var zoom = Zoom;
        if (size <= 0 || zoom <= 0)
            return;

        var scaled = size * zoom;
        if (scaled < 2)
            return; // 過度な密度は描画しない

        var rect = Bounds;

        // GridBrush を ImmutablePen 化
        var brush = Stroke ?? new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
        var pen = new Pen(brush);

        var offsetX = PosMod(PanOffset.X, scaled);
        var offsetY = PosMod(PanOffset.Y, scaled);

        using (context.PushTransform(Matrix.CreateTranslation(-0.5, -0.5)))
        {
            // 垂直線
            var startX = rect.X + offsetX;
            var endX = rect.X + rect.Width;
            for (var x = startX; x < endX; x += scaled)
            {
                var px = x + 0.5;
                var p0 = new Point(px, rect.Y + 0.5);
                var p1 = new Point(px, rect.Y + rect.Height + 0.5);
                context.DrawLine(pen, p0, p1);
            }

            // 水平線
            var startY = rect.Y + offsetY;
            var endY = rect.Y + rect.Height;
            for (var y = startY; y < endY; y += scaled)
            {
                var py = y + 0.5;
                var p0 = new Point(rect.X + 0.5, py);
                var p1 = new Point(rect.X + rect.Width + 0.5, py);
                context.DrawLine(pen, p0, p1);
            }
        }

        return;

        static double PosMod(double a, double m)
        {
            return (a % m + m) % m;
        }
    }
}