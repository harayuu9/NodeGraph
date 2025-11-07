using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
using NodeGraph.Editor.Primitives;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Services;
using NodeGraph.Editor.Views;
using NodeGraph.Model;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlはEditorGraphを視覚化し、パンとズーム機能を提供します
/// </summary>
public class GraphControl : TemplatedControl
{
    private Canvas? _canvas;
    private Canvas? _overlayCanvas;
    private Canvas? _uiCanvas;
    private GridDecorator? _gridDecorator;
    private Point _lastDragPoint;
    private bool _isDragging;
    private bool _isRightButtonDown;
    private Point _rightButtonDownPoint;

    // コネクタ管理
    private bool _connectorUpdateScheduled;

    // 矩形選択用
    private bool _isSelecting;
    private Point _selectionStartPoint;

    // ポートドラッグ用
    private bool _isDraggingPort;
    private PortControl? _dragSourcePort;
    private PortControl? _currentHoverPort;

    // トランスフォーム
    private readonly TranslateTransform _translateTransform = new();
    private readonly ScaleTransform _scaleTransform = new() { ScaleX = 1.0, ScaleY = 1.0 };
    private readonly TransformGroup _transformGroup;

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
        
        if (Design.IsDesignMode)
        {
            // テスト用のグラフを作成
            var graph = new Graph();

            // テスト用のノードを作成
            var a1 = graph.CreateNode<FloatConstantNode>();
            a1.SetValue(10);
        
            var a2 = graph.CreateNode<FloatConstantNode>();
            a2.SetValue(5);
        
            var add = graph.CreateNode<FloatAddNode>();
            add.ConnectInput(0, a1, 0);
            add.ConnectInput(1, a2, 0);
        
            graph.CreateNode<FloatResultNode>();
            
            var testGraph = new EditorGraph(graph, new SelectionManager());

            // ノードの位置を設定
            testGraph.Nodes[0].X = 100;
            testGraph.Nodes[0].Y = 100;

            testGraph.Nodes[1].X = 350;
            testGraph.Nodes[1].Y = 50;

            testGraph.Nodes[2].X = 350;
            testGraph.Nodes[2].Y = 200;

            testGraph.Nodes[3].X = 600;
            testGraph.Nodes[3].Y = 120;
            Graph = testGraph;
        }
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

    public static readonly StyledProperty<Vector> PanOffsetProperty = AvaloniaProperty.Register<GraphControl, Vector>(nameof(PanOffset), default);

    public Vector PanOffset
    {
        get => GetValue(PanOffsetProperty);
        set => SetValue(PanOffsetProperty, value);
    }

    public static readonly StyledProperty<bool> IsSelectionVisibleProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(IsSelectionVisible), false);

    public bool IsSelectionVisible
    {
        get => GetValue(IsSelectionVisibleProperty);
        set => SetValue(IsSelectionVisibleProperty, value);
    }

    public static readonly StyledProperty<Rect> SelectionRectProperty = AvaloniaProperty.Register<GraphControl, Rect>(nameof(SelectionRect), default);

    public Rect SelectionRect
    {
        get => GetValue(SelectionRectProperty);
        set => SetValue(SelectionRectProperty, value);
    }

    public static readonly StyledProperty<bool> IsDraggingConnectorProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(IsDraggingConnector), false);

    public bool IsDraggingConnector
    {
        get => GetValue(IsDraggingConnectorProperty);
        set => SetValue(IsDraggingConnectorProperty, value);
    }

    public static readonly StyledProperty<ConnectorLine> DragConnectorLineProperty = AvaloniaProperty.Register<GraphControl, ConnectorLine>(nameof(DragConnectorLine), ConnectorLine.Zero);

    public ConnectorLine DragConnectorLine
    {
        get => GetValue(DragConnectorLineProperty);
        set => SetValue(DragConnectorLineProperty, value);
    }

    public static readonly StyledProperty<string?> DragConnectorPortTypeProperty = AvaloniaProperty.Register<GraphControl, string?>(nameof(DragConnectorPortType), null);

    public string? DragConnectorPortType
    {
        get => GetValue(DragConnectorPortTypeProperty);
        set => SetValue(DragConnectorPortTypeProperty, value);
    }

    public static readonly StyledProperty<bool> ShowGridProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(ShowGrid), true);

    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public static readonly StyledProperty<double> GridSizeProperty = AvaloniaProperty.Register<GraphControl, double>(nameof(GridSize), 20.0);

    public double GridSize
    {
        get => GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    public static readonly StyledProperty<IBrush?> GridBrushProperty = AvaloniaProperty.Register<GraphControl, IBrush?>(nameof(GridBrush), new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)));

    public IBrush? GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    #endregion

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _overlayCanvas = e.NameScope.Find<Canvas>("PART_OverlayCanvas");
        _uiCanvas = e.NameScope.Find<Canvas>("PART_UICanvas");
        _gridDecorator = e.NameScope.Find<GridDecorator>("PART_GridDecorator");

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

        // グリッドは Render 時に Pan/Zoom を考慮して描画するため、
        // RenderTransform は適用しない。
        if (_gridDecorator != null)
        {
            UpdateGrid();
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
            UpdateGrid();
        }
        else if (change.Property == PanOffsetProperty)
        {
            UpdatePan();
            UpdateGrid();
        }
        else if (change.Property == ShowGridProperty || change.Property == GridSizeProperty || change.Property == GridBrushProperty)
        {
            UpdateGrid();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateGrid();
    }

    #region Pan and Zoom Implementation

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // フォーカスを取得（Deleteキーなどを受け取るため）
        Focus();

        var properties = e.GetCurrentPoint(this).Properties;

        // 右ボタンの処理（ドラッグとコンテキストメニューの判定のため）
        if (properties.IsRightButtonPressed)
        {
            _isRightButtonDown = true;
            _rightButtonDownPoint = e.GetPosition(this);
            _lastDragPoint = _rightButtonDownPoint;
            e.Handled = true;
            return;
        }

        // 中ボタンでドラッグ開始
        if (properties.IsMiddleButtonPressed)
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

                IsSelectionVisible = true;
                SelectionRect = new Rect(_selectionStartPoint, new Size(0, 0));

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
        // 右ボタンが押されていてドラッグされた場合はパンモードに移行
        if (_isRightButtonDown && !_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _rightButtonDownPoint;
            var distance = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

            // 一定距離以上移動したらドラッグモードに移行
            if (distance > 5)
            {
                _isDragging = true;
                _isRightButtonDown = false;
                e.Pointer.Capture(this);
                OnPanStarted();
            }
        }

        if (_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _lastDragPoint;

            PanOffset += delta;

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
        else if (_isDraggingPort && _canvas != null)
        {
            // ポートドラッグ中の一時的な接続線を更新
            var currentPoint = e.GetPosition(_canvas);

            if (_dragSourcePort?.Port?.IsOutput == true)
            {
                // Output portからドラッグしている場合、終点を更新
                DragConnectorLine = new ConnectorLine(DragConnectorLine.Start, currentPoint);
            }
            else
            {
                // Input portからドラッグしている場合、始点を更新
                DragConnectorLine = new ConnectorLine(currentPoint, DragConnectorLine.End);
            }

            // マウス位置のポートを検索してハイライト
            UpdatePortHighlight(e.GetPosition(this));
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;

        // 右ボタンが離された場合
        if (_isRightButtonDown && properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
        {
            _isRightButtonDown = false;

            // ドラッグしていない場合はAddNodeWindowを表示
            if (!_isDragging)
            {
                _ = ShowAddNodeWindow(e.GetPosition(this));
            }

            e.Handled = true;
            return;
        }

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
            IsSelectionVisible = false;

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

        var pointerPosition = e.GetPosition(this);
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
        var newPanX = point.X - (point.X - PanOffset.X) * ratio;
        var newPanY = point.Y - (point.Y - PanOffset.Y) * ratio;
        PanOffset = new Vector(newPanX, newPanY);
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
        _translateTransform.X = PanOffset.X;
        _translateTransform.Y = PanOffset.Y;
    }

    private void UpdateSelectionRectangle(Point start, Point end)
    {
        var topLeft = new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
        var bottomRight = new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
        SelectionRect = new Rect(topLeft, bottomRight);
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
                var nodeControl = FindNodeControl(node);
                if (nodeControl == null)
                    return false;

                var nodeRect = new Rect(node.X, node.Y, nodeControl.Bounds.Width, nodeControl.Bounds.Height);
                return selectionRect.Intersects(nodeRect);
            })
            .Cast<Selection.ISelectable>();

        // 矩形内の接続を検出（始点または終点が矩形内にある）
        var selectedConnections = GetAllConnectorControls()
            .Where(connector =>
            {
                var startPoint = new Point(connector.StartX, connector.StartY);
                var endPoint = new Point(connector.EndX, connector.EndY);
                return selectionRect.Contains(startPoint) || selectionRect.Contains(endPoint);
            })
            .Where(connector => connector.Connection != null)
            .Select(connector => connector.Connection!)
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
        var connectors = GetAllConnectorControls();
        foreach (var connector in connectors)
        {
            if (connector.Connection != null)
            {
                connector.IsSelected = Graph?.SelectionManager.IsSelected(connector.Connection) ?? false;
            }
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

        var connectors = GetAllConnectorControls();
        foreach (var connector in connectors)
        {
            if (connector.Connection == null)
                continue;

            var connection = connector.Connection;
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
    /// ItemsControlで生成されたすべてのConnectorControlを取得します
    /// </summary>
    private IEnumerable<ConnectorControl> GetAllConnectorControls()
    {
        return _overlayCanvas == null ? [] : _overlayCanvas.GetVisualDescendants().OfType<ConnectorControl>();
    }

    /// <summary>
    /// 指定されたEditorNodeに対応するNodeControlを検索します
    /// </summary>
    private NodeControl? FindNodeControl(EditorNode node)
    {
        return _canvas?.Children
            .OfType<NodeControl>()
            .FirstOrDefault(nc => nc.Node == node);
    }

    /// <summary>
    /// 指定されたノードとポートの画面上の座標を取得します
    /// </summary>
    private Point? GetPortPosition(EditorNode node, EditorPort port)
    {
        // ノードに対応するNodeControlを検索
        var nodeControl = FindNodeControl(node);

        if (nodeControl == null)
        {
            return null;
        }

        // PortControlを検索
        var portControl = FindPortControl(nodeControl, port);

        // INFO 本来は_canvas?.Childrenで見てるから、ここでnullな分けないんだけど構文解析がアホだから CS8604のワーニング出す
        if (_canvas == null)
        {
            return null;
        }
        
        // PortControlが自身で中心座標を解決するAPIを使用
        return portControl?.GetCenterIn(_canvas);
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
        if (Graph == null)
            return;

        // 選択されている接続を取得
        var selectedConnections = Graph.SelectionManager.SelectedItems
            .OfType<EditorConnection>()
            .ToList();

        if (selectedConnections.Count == 0)
            return;

        // 選択を解除
        Graph.SelectionManager.ClearSelection();

        // 接続を削除（ItemsControlが自動的にConnectorControlを削除します）
        foreach (var connection in selectedConnections)
        {
            // モデルレベルで接続を切断
            connection.TargetPort.Port.DisconnectAll();

            // EditorConnectionを削除
            Graph.Connections.Remove(connection);
        }
    }

    #endregion

    #region Port Drag Handling

    private void OnPortDragStarted(object? sender, RoutedEventArgs e)
    {
        if (e is not PortDragEventArgs args || _canvas == null)
            return;

        _isDraggingPort = true;
        _dragSourcePort = args.PortControl;

        // ポートの中心座標を取得
        var portCenter = GetPortCenterPosition(args.PortControl);
        if (!portCenter.HasValue)
            return;

        // ドラッグ接続線の座標を設定
        DragConnectorLine = new ConnectorLine(portCenter.Value, portCenter.Value);

        // ドラッグ中のポートの型を設定
        DragConnectorPortType = args.PortControl.Port?.TypeName;

        // 一時的な接続線を表示
        IsDraggingConnector = true;
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
        DragConnectorPortType = null;

        // 一時的な接続線を非表示
        IsDraggingConnector = false;
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
        
        DisconnectIfSingleConnect(inputPort);
        DisconnectIfSingleConnect(outputPort);

        // モデルレベルで接続
        if (inputPort.Port.Connect(outputPort.Port))
        {
            // EditorConnectionを作成（ItemsControlが自動的にConnectorControlを生成します）
            var connection = new EditorConnection(outputNode, outputPort, inputNode, inputPort);
            Graph.Connections.Add(connection);

            // 座標を更新
            ScheduleConnectorUpdate();
        }

        return;

        void DisconnectIfSingleConnect(EditorPort port)
        {
            if (port.Port is SingleConnectPort singleConnectPort)
            {
                var old = singleConnectPort.ConnectedPort;
                if (old != null)
                {
                    var node = Graph!.Nodes.FirstOrDefault(n => n.InputPorts.Contains(port));
                    node ??= Graph!.Nodes.FirstOrDefault(n => n.OutputPorts.Contains(port));
                    if (node != null)
                    {
                        var oldConnection = Graph!.Connections.FirstOrDefault(c => c.TargetNode == node && c.TargetPort == port);
                        if (oldConnection != null)
                        {
                            Graph.Connections.Remove(oldConnection);
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region Grid Drawing

    private void UpdateGrid()
    {
        // Decorator に再描画を依頼（プロパティはXAMLバインドで渡される）
        _gridDecorator?.InvalidateVisual();
    }

    #endregion

    #region Add Node

    private async Task ShowAddNodeWindow(Point clickPosition)
    {
        if (Graph == null || _canvas == null)
            return;

        // NodeTypeServiceを取得
        var app = Application.Current as App;
        var nodeTypeService = app?.Services?.GetService(typeof(NodeTypeService)) as NodeTypeService;

        if (nodeTypeService == null)
            return;

        // 親ウィンドウを取得
        var window = this.GetVisualRoot() as Window;
        if (window == null)
            return;

        // スクリーン座標を取得
        var screenPosition = window.PointToScreen(clickPosition);

        // AddNodeWindowを表示
        var selectedNodeType = await AddNodeWindow.ShowDialog(window, screenPosition, nodeTypeService);

        if (selectedNodeType != null)
        {
            // キャンバス座標に変換
            var canvasPosition = this.TranslatePoint(clickPosition, _canvas);
            if (canvasPosition.HasValue)
            {
                CreateNode(selectedNodeType, canvasPosition.Value);
            }
        }
    }

    private void CreateNode(NodeTypeInfo nodeTypeInfo, Point position)
    {
        if (Graph == null)
            return;

        // リフレクションを使用してノードを作成
        var createNodeMethod = typeof(Model.Graph).GetMethod(nameof(Model.Graph.CreateNode));
        if (createNodeMethod == null)
            return;

        var genericMethod = createNodeMethod.MakeGenericMethod(nodeTypeInfo.NodeType);
        var node = genericMethod.Invoke(Graph.Graph, null) as Node;

        if (node == null)
            return;

        // EditorNodeを作成して追加
        var editorNode = new EditorNode(Graph.SelectionManager, node)
        {
            X = position.X,
            Y = position.Y
        };

        Graph.Nodes.Add(editorNode);

        // NodeControlを作成して表示
        if (_canvas != null)
        {
            var nodeControl = CreateNodeControl(editorNode);
            Canvas.SetLeft(nodeControl, editorNode.X);
            Canvas.SetTop(nodeControl, editorNode.Y);
            _canvas.Children.Add(nodeControl);

            // プロパティ変更イベントを監視
            editorNode.PropertyChanged += OnNodePropertyChanged;

            // コネクタを更新
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
        var panX = (Bounds.Width - rect.Width * Zoom) / 2 - rect.X * Zoom;
        var panY = (Bounds.Height - rect.Height * Zoom) / 2 - rect.Y * Zoom;
        PanOffset = new Vector(panX, panY);
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
            var nodeControl = FindNodeControl(node);
            if (nodeControl == null)
                continue;

            minX = Math.Min(minX, node.X);
            minY = Math.Min(minY, node.Y);
            maxX = Math.Max(maxX, node.X + nodeControl.Bounds.Width);
            maxY = Math.Max(maxY, node.Y + nodeControl.Bounds.Height);
        }

        FitToRect(new Rect(minX, minY, maxX - minX, maxY - minY));
    }

    /// <summary>
    /// ズームとパンをリセットします
    /// </summary>
    public void ResetView()
    {
        Zoom = 1.0;
        PanOffset = default;
    }

    #endregion
}
