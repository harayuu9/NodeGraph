using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlはEditorGraphを視覚化し、パンとズーム機能を提供します
/// </summary>
public class GraphControl : TemplatedControl
{
    private Canvas? _canvas;
    private Canvas? _overlayCanvas;
    private Canvas? _uiCanvas;
    private Point _lastDragPoint;
    private bool _isDragging;

    // コネクタ管理
    private readonly Dictionary<EditorConnection, ConnectorControl> _connectorControls = new();
    private bool _connectorUpdateScheduled;

    // 矩形選択用
    private bool _isSelecting;
    private Point _selectionStartPoint;
    private Rectangle? _selectionRectangle;

    // トランスフォーム
    private TranslateTransform _translateTransform = new();
    private ScaleTransform _scaleTransform = new() { ScaleX = 1.0, ScaleY = 1.0 };
    private TransformGroup _transformGroup;

    public GraphControl()
    {
        _transformGroup = new TransformGroup();
        _transformGroup.Children.Add(_scaleTransform);
        _transformGroup.Children.Add(_translateTransform);

        // イベントハンドラの登録
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    #region Styled Properties

    public static readonly StyledProperty<EditorGraph?> GraphProperty = AvaloniaProperty.Register<GraphControl, EditorGraph?>(nameof(Graph));

    public EditorGraph? Graph
    {
        get => GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    public static readonly StyledProperty<double> ZoomProperty = AvaloniaProperty.Register<GraphControl, double>(nameof(Zoom), 1.0);

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, Math.Clamp(value, MinZoom, MaxZoom));
    }

    public static readonly StyledProperty<double> MinZoomProperty = AvaloniaProperty.Register<GraphControl, double>(nameof(MinZoom), 0.1);

    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public static readonly StyledProperty<double> MaxZoomProperty = AvaloniaProperty.Register<GraphControl, double>(nameof(MaxZoom), 5.0);

    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public static readonly StyledProperty<double> PanXProperty = AvaloniaProperty.Register<GraphControl, double>(nameof(PanX), 0.0);

    public double PanX
    {
        get => GetValue(PanXProperty);
        set => SetValue(PanXProperty, value);
    }

    public static readonly StyledProperty<double> PanYProperty = AvaloniaProperty.Register<GraphControl, double>(nameof(PanY), 0.0);

    public double PanY
    {
        get => GetValue(PanYProperty);
        set => SetValue(PanYProperty, value);
    }

    #endregion

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _overlayCanvas = e.NameScope.Find<Canvas>("PART_OverlayCanvas");
        _uiCanvas = e.NameScope.Find<Canvas>("PART_UICanvas");

        if (_canvas != null)
        {
            _canvas.RenderTransform = _transformGroup;
            OnGraphChanged();
        }

        // オーバーレイキャンバスにも同じトランスフォームを適用（コネクタ用）
        if (_overlayCanvas != null)
        {
            _overlayCanvas.RenderTransform = _transformGroup;
        }

        // UIキャンバスにはトランスフォームを適用しない（選択矩形用）
        if (_uiCanvas != null)
        {
            // 選択矩形の初期化
            _selectionRectangle = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(50, 100, 150, 255)),
                Stroke = new SolidColorBrush(Color.FromArgb(200, 100, 150, 255)),
                StrokeThickness = 1,
                IsVisible = false
            };
            _uiCanvas.Children.Add(_selectionRectangle);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GraphProperty)
        {
            OnGraphChanged();
        }
        else if (change.Property == ZoomProperty)
        {
            UpdateZoom();
        }
        else if (change.Property == PanXProperty || change.Property == PanYProperty)
        {
            UpdatePan();
        }
    }

    #region Pan and Zoom Implementation

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;

        // 中ボタンまたは右ボタンでドラッグ開始
        if (properties.IsMiddleButtonPressed || properties.IsRightButtonPressed)
        {
            _isDragging = true;
            _lastDragPoint = e.GetPosition(this);
            e.Pointer.Capture(this);
            e.Handled = true;

            OnPanStarted();
        }
        else if (properties.IsLeftButtonPressed)
        {
            // NodeControl上でのクリックでない場合のみ矩形選択を開始
            if (e.Source is not NodeControl)
            {
                _isSelecting = true;
                _selectionStartPoint = e.GetPosition(this);

                if (_selectionRectangle != null)
                {
                    _selectionRectangle.IsVisible = true;
                    Canvas.SetLeft(_selectionRectangle, _selectionStartPoint.X);
                    Canvas.SetTop(_selectionRectangle, _selectionStartPoint.Y);
                    _selectionRectangle.Width = 0;
                    _selectionRectangle.Height = 0;
                }

                e.Pointer.Capture(this);
                e.Handled = true;

                // Ctrlキーが押されていない場合は選択をクリア
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    Graph?.SelectionManager.ClearSelection();
                }
            }
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _lastDragPoint;

            PanX += delta.X;
            PanY += delta.Y;

            _lastDragPoint = currentPoint;
            e.Handled = true;

            OnPanning(delta);
        }
        else if (_isSelecting)
        {
            var currentPoint = e.GetPosition(this);
            UpdateSelectionRectangle(_selectionStartPoint, currentPoint);

            // ドラッグ中もリアルタイムで選択を更新
            SelectNodesInRectangle(_selectionStartPoint, currentPoint, e.KeyModifiers);

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

            OnPanEnded();
        }
        else if (_isSelecting)
        {
            _isSelecting = false;

            _selectionRectangle?.IsVisible = false;

            var currentPoint = e.GetPosition(this);
            SelectNodesInRectangle(_selectionStartPoint, currentPoint, e.KeyModifiers);

            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        var zoomFactor = delta > 0 ? 1.1 : 0.9;

        var pointerPosition = e.GetPosition(_canvas);
        ZoomAtPoint(pointerPosition, zoomFactor);

        e.Handled = true;
    }

    private void ZoomAtPoint(Point point, double factor)
    {
        var oldZoom = Zoom;
        var newZoom = Math.Clamp(oldZoom * factor, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - oldZoom) < 0.001)
            return;

        // ポイントを中心にズーム
        var ratio = newZoom / oldZoom;
        PanX = point.X - (point.X - PanX) * ratio;
        PanY = point.Y - (point.Y - PanY) * ratio;
        Zoom = newZoom;

        OnZoomChanged(oldZoom, newZoom, point);
    }

    private void UpdateZoom()
    {
        _scaleTransform.ScaleX = Zoom;
        _scaleTransform.ScaleY = Zoom;
    }

    private void UpdatePan()
    {
        _translateTransform.X = PanX;
        _translateTransform.Y = PanY;
    }

    private void UpdateSelectionRectangle(Point start, Point end)
    {
        if (_selectionRectangle == null)
            return;

        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        Canvas.SetLeft(_selectionRectangle, left);
        Canvas.SetTop(_selectionRectangle, top);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;
    }

    private void SelectNodesInRectangle(Point start, Point end, KeyModifiers modifiers)
    {
        if (Graph == null || _canvas == null)
            return;

        // 選択矩形をビューポート座標からキャンバス座標に変換
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var right = Math.Max(start.X, end.X);
        var bottom = Math.Max(start.Y, end.Y);

        // GraphControlの座標を_canvasの座標に変換
        var topLeftCanvas = this.TranslatePoint(new Point(left, top), _canvas);
        var bottomRightCanvas = this.TranslatePoint(new Point(right, bottom), _canvas);

        if (!topLeftCanvas.HasValue || !bottomRightCanvas.HasValue)
            return;

        var selectionRect = new Rect(topLeftCanvas.Value, bottomRightCanvas.Value);

        // 矩形内のノードを検出
        var selectedNodes = Graph.Nodes
            .Where(node =>
            {
                var nodeRect = new Rect(node.PositionX, node.PositionY, node.Width, node.Height);
                return selectionRect.Intersects(nodeRect);
            })
            .ToList();

        // Ctrlキーが押されている場合は既存の選択に追加、そうでなければ新規選択
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            foreach (var node in selectedNodes)
            {
                Graph.SelectionManager.AddToSelection(node);
            }
        }
        else
        {
            Graph.SelectionManager.SelectRange(selectedNodes);
        }
    }

    #endregion

    #region Graph Management

    private void OnGraphChanged()
    {
        if (_canvas == null || Graph == null)
            return;

        _canvas.Children.Clear();
        _connectorControls.Clear();

        // 既存のノードのイベントハンドラを解除
        foreach (var node in Graph.Nodes)
        {
            node.PropertyChanged -= OnNodePropertyChanged;
        }

        // ノードを作成
        foreach (var node in Graph.Nodes)
        {
            var nodeControl = CreateNodeControl(node);
            if (nodeControl != null)
            {
                Canvas.SetLeft(nodeControl, node.PositionX);
                Canvas.SetTop(nodeControl, node.PositionY);
                _canvas.Children.Add(nodeControl);
            }

            // ノードの位置変更を監視
            node.PropertyChanged += OnNodePropertyChanged;
        }

        // コネクタを作成
        if (_overlayCanvas != null)
        {
            // 選択矩形以外のコネクタをクリア
            var toRemove = _overlayCanvas.Children
                .OfType<ConnectorControl>()
                .ToList();
            foreach (var control in toRemove)
            {
                _overlayCanvas.Children.Remove(control);
            }

            foreach (var connection in Graph.Connections)
            {
                var connectorControl = new ConnectorControl
                {
                    Connection = connection
                };
                _connectorControls[connection] = connectorControl;
                _overlayCanvas.Children.Add(connectorControl);
            }
        }

        // 初期座標を設定（レイアウト後に更新）
        Dispatcher.UIThread.Post(UpdateConnectors, DispatcherPriority.Render);
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorNode.PositionX) or nameof(EditorNode.PositionY))
        {
            ScheduleConnectorUpdate();
        }
    }

    /// <summary>
    /// コネクタの更新をスケジュールします（次のレンダリングフレームで実行）
    /// </summary>
    private void ScheduleConnectorUpdate()
    {
        if (_connectorUpdateScheduled)
            return;

        _connectorUpdateScheduled = true;
        Dispatcher.UIThread.Post(() =>
        {
            _connectorUpdateScheduled = false;
            UpdateConnectors();
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// EditorNodeからNodeControlを作成します
    /// オーバーライドしてカスタムノードコントロールを作成できます
    /// </summary>
    protected virtual Control? CreateNodeControl(EditorNode node)
    {
        return new NodeControl { Node = node };
    }

    /// <summary>
    /// すべてのコネクタの座標を更新します
    /// </summary>
    private void UpdateConnectors()
    {
        if (_canvas == null || _overlayCanvas == null || Graph == null)
            return;

        foreach (var (connection, connector) in _connectorControls)
        {
            var startPos = GetPortPosition(connection.SourceNode, connection.SourcePort);
            var endPos = GetPortPosition(connection.TargetNode, connection.TargetPort);

            if (startPos.HasValue && endPos.HasValue)
            {
                connector.StartX = startPos.Value.X + 4;
                connector.StartY = startPos.Value.Y;
                connector.EndX = endPos.Value.X - 4;
                connector.EndY = endPos.Value.Y;
            }
        }
    }

    /// <summary>
    /// 指定されたノードとポートの画面上の座標を取得します
    /// </summary>
    private Point? GetPortPosition(EditorNode node, EditorPort port)
    {
        if (_canvas == null)
        {
            return null;
        }

        // ノードに対応するNodeControlを検索
        var nodeControl = _canvas.Children
            .OfType<NodeControl>()
            .FirstOrDefault(nc => nc.Node == node);

        if (nodeControl == null)
        {
            return null;
        }

        // PortControlを検索
        var portControl = FindPortControl(nodeControl, port);

        // PortControl内のEllipseを検索
        var ellipse = portControl?.GetVisualDescendants()
            .OfType<Ellipse>()
            .FirstOrDefault();

        if (ellipse == null)
        {
            return null;
        }

        // Ellipseの中心座標を_canvas座標系で取得
        var ellipseBounds = ellipse.Bounds;
        var centerInEllipse = new Point(ellipseBounds.Width / 2, ellipseBounds.Height / 2);

        // Ellipseから_canvasへの座標変換
        // _canvasと_overlayCanvasは同じトランスフォームを共有しているため、
        // _canvas座標系で取得した座標はそのまま_overlayCanvasでも使用可能
        var centerInCanvas = ellipse.TranslatePoint(centerInEllipse, _canvas);

        return centerInCanvas;
    }

    /// <summary>
    /// NodeControl内から指定されたEditorPortに対応するPortControlを検索します
    /// </summary>
    private static PortControl? FindPortControl(NodeControl nodeControl, EditorPort port)
    {
        // VisualTreeをトラバースしてPortControlを検索
        // ItemsControlで生成されたコントロールはVisualTreeに配置される
        return nodeControl.GetVisualDescendants()
            .OfType<PortControl>()
            .FirstOrDefault(pc => pc.Port == port);
    }

    #endregion

    #region Extensibility Points

    /// <summary>
    /// パン操作が開始されたときに呼び出されます
    /// </summary>
    protected virtual void OnPanStarted()
    {
    }

    /// <summary>
    /// パン操作中に呼び出されます
    /// </summary>
    protected virtual void OnPanning(Vector delta)
    {
        ScheduleConnectorUpdate();
    }

    /// <summary>
    /// パン操作が終了したときに呼び出されます
    /// </summary>
    protected virtual void OnPanEnded()
    {
    }

    /// <summary>
    /// ズームが変更されたときに呼び出されます
    /// </summary>
    protected virtual void OnZoomChanged(double oldZoom, double newZoom, Point zoomCenter)
    {
        ScheduleConnectorUpdate();
    }

    #endregion

    #region Public API

    /// <summary>
    /// ビューを指定された矩形にフィットさせます
    /// </summary>
    public void FitToRect(Rect rect)
    {
        if (Bounds.Width == 0 || Bounds.Height == 0)
            return;

        var scaleX = Bounds.Width / rect.Width;
        var scaleY = Bounds.Height / rect.Height;
        var scale = Math.Min(scaleX, scaleY) * 0.9; // 少し余白を持たせる

        Zoom = Math.Clamp(scale, MinZoom, MaxZoom);
        PanX = (Bounds.Width - rect.Width * Zoom) / 2 - rect.X * Zoom;
        PanY = (Bounds.Height - rect.Height * Zoom) / 2 - rect.Y * Zoom;
    }

    /// <summary>
    /// すべてのノードが見えるようにビューをフィットさせます
    /// </summary>
    public void FitToContent()
    {
        if (Graph == null || Graph.Nodes.Count == 0)
            return;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var node in Graph.Nodes)
        {
            minX = Math.Min(minX, node.PositionX);
            minY = Math.Min(minY, node.PositionY);
            maxX = Math.Max(maxX, node.PositionX + node.Width);
            maxY = Math.Max(maxY, node.PositionY + node.Height);
        }

        FitToRect(new Rect(minX, minY, maxX - minX, maxY - minY));
    }

    /// <summary>
    /// ズームとパンをリセットします
    /// </summary>
    public void ResetView()
    {
        Zoom = 1.0;
        PanX = 0;
        PanY = 0;
    }

    #endregion
}
