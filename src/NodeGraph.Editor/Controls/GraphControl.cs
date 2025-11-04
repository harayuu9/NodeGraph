using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    // ポートドラッグ用
    private bool _isDraggingPort;
    private PortControl? _dragSourcePort;
    private ConnectorControl? _dragConnector;
    private PortControl? _currentHoverPort;

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
        KeyDown += OnKeyDown;

        // ポートドラッグイベントのハンドラ登録
        AddHandler(PortControl.PortDragStartedEvent, OnPortDragStarted);

        // フォーカス可能にする
        Focusable = true;
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
            // テーマリソースから色を取得
            var fillBrush = this.FindResource("SelectionFillBrush") as IBrush ??
                           new SolidColorBrush(Color.FromArgb(50, 100, 150, 255));
            var strokeBrush = this.FindResource("SelectionStrokeBrush") as IBrush ??
                             new SolidColorBrush(Color.FromArgb(200, 100, 150, 255));

            // 選択矩形の初期化
            _selectionRectangle = new Rectangle
            {
                Fill = fillBrush,
                Stroke = strokeBrush,
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
        // フォーカスを取得（Deleteキーなどを受け取るため）
        Focus();

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
            // NodeControl上またはポートドラッグ中でのクリックでない場合のみ矩形選択を開始
            if (e.Source is not NodeControl && !_isDraggingPort)
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
        else if (_isDraggingPort && _dragConnector != null && _canvas != null)
        {
            // ポートドラッグ中の一時的な接続線を更新
            var currentPoint = e.GetPosition(_canvas);

            if (_dragSourcePort?.Port?.IsOutput == true)
            {
                // Output portからドラッグしている場合、終点を更新
                _dragConnector.EndX = currentPoint.X;
                _dragConnector.EndY = currentPoint.Y;
            }
            else
            {
                // Input portからドラッグしている場合、始点を更新
                _dragConnector.StartX = currentPoint.X;
                _dragConnector.StartY = currentPoint.Y;
            }

            // マウス位置のポートを検索してハイライト
            UpdatePortHighlight(e.GetPosition(this));
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

            if (_selectionRectangle != null)
            {
                _selectionRectangle.IsVisible = false;
            }

            var currentPoint = e.GetPosition(this);
            SelectNodesInRectangle(_selectionStartPoint, currentPoint, e.KeyModifiers);

            e.Pointer.Capture(null);
            e.Handled = true;
        }
        else if (_isDraggingPort)
        {
            // ポートドラッグ完了
            CompletePortDrag();
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

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && Graph != null)
        {
            DeleteSelectedConnections();
            e.Handled = true;
        }
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
                var nodeRect = new Rect(node.X, node.Y, node.Width, node.Height);
                return selectionRect.Intersects(nodeRect);
            })
            .Cast<Selection.ISelectable>();

        // 矩形内の接続を検出（始点または終点が矩形内にある）
        var selectedConnections = _connectorControls
            .Where(kvp =>
            {
                var connector = kvp.Value;
                var startPoint = new Point(connector.StartX, connector.StartY);
                var endPoint = new Point(connector.EndX, connector.EndY);
                return selectionRect.Contains(startPoint) || selectionRect.Contains(endPoint);
            })
            .Select(kvp => kvp.Key)
            .Cast<Selection.ISelectable>();

        // ノードと接続を結合
        var selectedItems = selectedNodes.Concat(selectedConnections).ToList();

        // Ctrlキーが押されている場合は既存の選択に追加、そうでなければ新規選択
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            foreach (var item in selectedItems)
            {
                Graph.SelectionManager.AddToSelection(item);
            }
        }
        else
        {
            Graph.SelectionManager.SelectRange(selectedItems);
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

        // 既存のSelectionManagerのイベントハンドラを解除
        Graph.SelectionManager.SelectionChanged -= OnSelectionChanged;

        // 既存のノードのイベントハンドラを解除
        foreach (var node in Graph.Nodes)
        {
            node.PropertyChanged -= OnNodePropertyChanged;
        }

        // SelectionManagerのイベントハンドラを登録
        Graph.SelectionManager.SelectionChanged += OnSelectionChanged;

        // ノードを作成
        foreach (var node in Graph.Nodes)
        {
            var nodeControl = CreateNodeControl(node);
            Canvas.SetLeft(nodeControl, node.X);
            Canvas.SetTop(nodeControl, node.Y);
            _canvas.Children.Add(nodeControl);

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
        if (e.PropertyName is nameof(EditorNode.X) or nameof(EditorNode.Y))
        {
            ScheduleConnectorUpdate();
        }
    }

    private void OnSelectionChanged(object? sender, Selection.SelectionChangedEventArgs e)
    {
        // ConnectorControlの選択状態を更新
        foreach (var (connection, connector) in _connectorControls)
        {
            connector.IsSelected = Graph?.SelectionManager.IsSelected(connection) ?? false;
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
    protected virtual Control CreateNodeControl(EditorNode node)
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

        if (portControl == null)
        {
            return null;
        }

        // PortControlが自身で中心座標を解決するAPIを使用
        return portControl.GetCenterIn(_canvas);
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

    #region Connection Management

    private void DeleteSelectedConnections()
    {
        if (Graph == null || _overlayCanvas == null)
            return;

        // 選択されている接続を取得
        var selectedConnections = Graph.SelectionManager.SelectedItems
            .OfType<EditorConnection>()
            .ToList();

        if (selectedConnections.Count == 0)
            return;

        // 選択を解除
        Graph.SelectionManager.ClearSelection();

        // 接続を削除
        foreach (var connection in selectedConnections)
        {
            // モデルレベルで接続を切断
            connection.TargetPort.Port.DisconnectAll();

            // EditorConnectionを削除
            Graph.Connections.Remove(connection);

            // ConnectorControlを削除
            if (_connectorControls.TryGetValue(connection, out var connectorControl))
            {
                _overlayCanvas.Children.Remove(connectorControl);
                _connectorControls.Remove(connection);
            }
        }
    }

    #endregion

    #region Port Drag Handling

    private void OnPortDragStarted(object? sender, RoutedEventArgs e)
    {
        if (e is not PortDragEventArgs args || _overlayCanvas == null || _canvas == null)
            return;

        _isDraggingPort = true;
        _dragSourcePort = args.PortControl;

        // ポートの中心座標を取得
        var portCenter = GetPortCenterPosition(args.PortControl);
        if (!portCenter.HasValue)
            return;

        // テーマリソースから色を取得
        var connectorBrush = this.FindResource("ConnectorStrokeBrush") as IBrush ?? new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));

        // 一時的な接続線を作成
        _dragConnector = new ConnectorControl
        {
            Stroke = connectorBrush,
            StrokeThickness = 2,
            IsHitTestVisible = false
        };

        if (args.Port.IsOutput)
        {
            // Output portからドラッグ開始
            _dragConnector.StartX = portCenter.Value.X;
            _dragConnector.StartY = portCenter.Value.Y;
            _dragConnector.EndX = portCenter.Value.X;
            _dragConnector.EndY = portCenter.Value.Y;
        }
        else
        {
            // Input portからドラッグ開始
            _dragConnector.StartX = portCenter.Value.X;
            _dragConnector.StartY = portCenter.Value.Y;
            _dragConnector.EndX = portCenter.Value.X;
            _dragConnector.EndY = portCenter.Value.Y;
        }

        _overlayCanvas.Children.Add(_dragConnector);
    }

    private void CompletePortDrag()
    {
        if (_dragSourcePort == null || Graph == null)
        {
            CleanupPortDrag();
            return;
        }

        var sourcePort = _dragSourcePort.Port;
        var targetPort = _currentHoverPort?.Port;

        if (sourcePort != null && targetPort != null && sourcePort != targetPort)
        {
            // 接続を作成
            CreateConnection(sourcePort, targetPort);
        }

        CleanupPortDrag();
    }

    private void UpdatePortHighlight(Point mousePosition)
    {
        if (_dragSourcePort?.Port == null)
            return;

        // マウス位置にあるPortControlを検索
        var portControl = GetPortAtPosition(mousePosition);

        // 前回ハイライトしていたポートをクリア
        if (_currentHoverPort != null && _currentHoverPort != portControl)
        {
            _currentHoverPort.IsHighlighted = false;
        }

        _currentHoverPort = null;

        // 新しいポートをハイライト（接続可能な場合のみ）
        if (portControl != null && portControl != _dragSourcePort && portControl.Port != null)
        {
            if (CanConnect(_dragSourcePort.Port, portControl.Port))
            {
                _currentHoverPort = portControl;
                _currentHoverPort.IsHighlighted = true;
            }
        }
    }

    private PortControl? GetPortAtPosition(Point position)
    {
        // ビジュアルツリーからマウス位置にあるコントロールを取得
        var element = this.InputHitTest(position);

        // PortControlまたはその子要素を見つける
        while (element != null)
        {
            if (element is PortControl portControl)
                return portControl;

            if (element is Visual visual)
            {
                var parent = visual.GetVisualParent();
                element = parent as IInputElement;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    private bool CanConnect(EditorPort sourcePort, EditorPort targetPort)
    {
        return sourcePort.Port.CanConnect(targetPort.Port);
    }

    private void CleanupPortDrag()
    {
        _isDraggingPort = false;

        // ハイライトをクリア
        if (_currentHoverPort != null)
        {
            _currentHoverPort.IsHighlighted = false;
            _currentHoverPort = null;
        }

        // ドラッグ状態をクリア
        _dragSourcePort = null;

        // 一時的な接続線を削除
        if (_dragConnector != null && _overlayCanvas != null)
        {
            _overlayCanvas.Children.Remove(_dragConnector);
            _dragConnector = null;
        }
    }

    private Point? GetPortCenterPosition(PortControl portControl)
    {
        if (_canvas == null)
            return null;

        return portControl.GetCenterIn(_canvas);
    }

    private void CreateConnection(EditorPort sourcePort, EditorPort targetPort)
    {
        if (Graph == null)
            return;

        // Output -> Input の順序を確保
        EditorPort outputPort, inputPort;

        if (sourcePort.IsOutput && targetPort.IsInput)
        {
            outputPort = sourcePort;
            inputPort = targetPort;
        }
        else if (sourcePort.IsInput && targetPort.IsOutput)
        {
            outputPort = targetPort;
            inputPort = sourcePort;
        }
        else
        {
            // 同じ種類のポート同士は接続できない
            return;
        }

        // EditorNodeを検索
        var outputNode = Graph.Nodes.FirstOrDefault(n => n.OutputPorts.Contains(outputPort));
        var inputNode = Graph.Nodes.FirstOrDefault(n => n.InputPorts.Contains(inputPort));

        if (outputNode == null || inputNode == null)
            return;

        // 既存の接続があればスキップ
        var existingConnection = Graph.Connections.FirstOrDefault(c =>
            c.SourceNode == outputNode && c.SourcePort == outputPort &&
            c.TargetNode == inputNode && c.TargetPort == inputPort);

        if (existingConnection != null)
            return;

        // モデルレベルで接続
        if (inputPort.Port.Connect(outputPort.Port))
        {
            // EditorConnectionを作成
            var connection = new EditorConnection(outputNode, outputPort, inputNode, inputPort);
            Graph.Connections.Add(connection);

            // ConnectorControlを作成
            var connectorControl = new ConnectorControl
            {
                Connection = connection
            };
            _connectorControls[connection] = connectorControl;
            _overlayCanvas?.Children.Add(connectorControl);

            // 座標を更新
            ScheduleConnectorUpdate();
        }
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
            minX = Math.Min(minX, node.X);
            minY = Math.Min(minY, node.Y);
            maxX = Math.Max(maxX, node.X + node.Width);
            maxY = Math.Max(maxY, node.Y + node.Height);
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
