using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using NodeGraph.Editor.Converters;
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
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<ConnectorControl, bool>(nameof(IsSelected));

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

    private IBrush? _normalStrokeBrush;
    private IBrush? _selectedStrokeBrush;
    private static readonly TypeToColorConverter _typeToColorConverter = new();

    public ConnectorControl()
    {
        // デフォルト値を設定（リソースが利用できない場合のフォールバック）
        Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));
        StrokeThickness = 2;
        IsHitTestVisible = true;
        Cursor = new Cursor(StandardCursorType.Hand);

        PointerPressed += OnPointerPressed;
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // テーマリソースから色を取得
        _normalStrokeBrush = this.FindResource("ConnectorStrokeBrush") as IBrush;
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
            {
                // Ctrlキー + クリックで選択トグル
                selectionManager.ToggleSelection(Connection);
            }
            else
            {
                // 通常のクリックで単一選択
                selectionManager.Select(Connection);
            }

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
            IBrush? typeBrush = null;

            if (Connection?.SourcePort?.TypeName is string typeName)
            {
                // TypeToColorConverter を使用して型に応じた色を取得
                typeBrush = _typeToColorConverter.Convert(typeName, typeof(IBrush), null, CultureInfo.InvariantCulture) as IBrush;
            }

            // 型に応じた色、またはテーマのデフォルト色、またはフォールバック色を使用
            Stroke = typeBrush ?? _normalStrokeBrush ?? new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));
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
}
