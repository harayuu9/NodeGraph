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
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Primitives;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Serialization;
using NodeGraph.Editor.Services;
using NodeGraph.Editor.Undo;
using NodeGraph.Editor.Views;
using NodeGraph.Model;
using NodeGraph.Model.Serialization;
using YamlDotNet.Serialization;

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
    private Border? _touchGuard;
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

    public static readonly StyledProperty<UndoRedoManager?> UndoRedoManagerProperty = AvaloniaProperty.Register<GraphControl, UndoRedoManager?>(nameof(UndoRedoManager));

    public UndoRedoManager? UndoRedoManager
    {
        get => GetValue(UndoRedoManagerProperty);
        set => SetValue(UndoRedoManagerProperty, value);
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

    public static readonly StyledProperty<bool> IsInputBlockedProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(IsInputBlocked), false);

    public bool IsInputBlocked
    {
        get => GetValue(IsInputBlockedProperty);
        set => SetValue(IsInputBlockedProperty, value);
    }

    #endregion

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 既存のタッチガードのイベントハンドラを解除
        if (_touchGuard != null)
        {
            _touchGuard.PointerPressed -= OnPointerPressed;
            _touchGuard.PointerMoved -= OnPointerMoved;
            _touchGuard.PointerReleased -= OnPointerReleased;
            _touchGuard.PointerWheelChanged -= OnPointerWheelChanged;
        }

        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _overlayCanvas = e.NameScope.Find<Canvas>("PART_OverlayCanvas");
        _uiCanvas = e.NameScope.Find<Canvas>("PART_UICanvas");
        _gridDecorator = e.NameScope.Find<GridDecorator>("PART_GridDecorator");
        _touchGuard = e.NameScope.Find<Border>("PART_TouchGuard");

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

        // タッチガードのイベントハンドラを登録（すべてのポインターイベントを処理済みにする）
        if (_touchGuard != null)
        {
            _touchGuard.PointerPressed += OnPointerPressed;
            _touchGuard.PointerMoved += OnPointerMoved;
            _touchGuard.PointerReleased += OnPointerReleased;
            _touchGuard.PointerWheelChanged += OnPointerWheelChanged;
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
        if (Graph == null)
        {
            return;
        }

        // フォーカスを取得（Deleteキーなどを受け取るため）
        Focus();

        var properties = e.GetCurrentPoint(this).Properties;

        // 右ボタンの処理（背景クリック時のAddNodeWindow表示用）
        if (properties.IsRightButtonPressed && !Graph.IsExecuting)
        {
            // NodeControl上でない場合のみ処理（ノードのコンテキストメニューと衝突しないように）
            if (e.Source is not NodeControl)
            {
                _isRightButtonDown = true;
                _rightButtonDownPoint = e.GetPosition(this);
                e.Handled = true;
            }
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

        if (properties.IsLeftButtonPressed && !Graph.IsExecuting)
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

            // AddNodeWindowを表示
            _ = ShowAddNodeWindow(e.GetPosition(this));

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
        else if (e.Key == Key.R && Graph != null)
        {
            ArrangeSelectedNodes();
            e.Handled = true;
        }
        else if (e.Key == Key.C && Graph != null)
        {
            CopySelectedNodes();
            e.Handled = true;
        }
        else if (e.Key == Key.V && Graph != null)
        {
            _ = PasteNodes();
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
        if (_canvas == null)
            return;

        _canvas.Children.Clear();

        if (Graph == null)
            return;

        // 既存のイベントハンドラを解除
        Graph.SelectionManager.SelectionChanged -= OnSelectionChanged;
        Graph.PropertyChanged -= OnGraphPropertyChanged;
        Graph.Nodes.CollectionChanged -= OnNodesCollectionChanged;

        // 既存のノードのイベントハンドラを解除
        foreach (var node in Graph.Nodes)
        {
            node.PropertyChanged -= OnNodePropertyChanged;
        }

        // イベントハンドラを登録
        Graph.SelectionManager.SelectionChanged += OnSelectionChanged;
        Graph.PropertyChanged += OnGraphPropertyChanged;
        Graph.Nodes.CollectionChanged += OnNodesCollectionChanged;

        // IsInputBlockedの初期値を設定
        IsInputBlocked = Graph.IsExecuting;

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

    private void OnGraphPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorGraph.IsExecuting))
        {
            IsInputBlocked = Graph?.IsExecuting ?? false;
        }
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

    private void OnNodesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_canvas == null)
            return;

        // 追加されたノードの処理
        if (e.NewItems != null)
        {
            foreach (EditorNode node in e.NewItems)
            {
                var nodeControl = CreateNodeControl(node);
                Canvas.SetLeft(nodeControl, node.X);
                Canvas.SetTop(nodeControl, node.Y);
                _canvas.Children.Add(nodeControl);

                // ノードの位置変更を監視
                node.PropertyChanged += OnNodePropertyChanged;
            }

            // コネクタを更新
            ScheduleConnectorUpdate();
        }

        // 削除されたノードの処理
        if (e.OldItems != null)
        {
            foreach (EditorNode node in e.OldItems)
            {
                // イベントハンドラを解除
                node.PropertyChanged -= OnNodePropertyChanged;

                // 対応するNodeControlを検索して削除
                var nodeControl = _canvas.Children.OfType<NodeControl>().FirstOrDefault(nc => nc.Node == node);
                if (nodeControl != null)
                {
                    _canvas.Children.Remove(nodeControl);
                }
            }

            // コネクタを更新
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

    #region Node Arrangement

    /// <summary>
    /// 選択されたノードをConnection情報に基づいて左から右に整列します
    /// </summary>
    private void ArrangeSelectedNodes()
    {
        if (Graph == null || _canvas == null)
            return;

        var selectedNodes = Graph.SelectionManager.SelectedItems
            .OfType<EditorNode>()
            .ToList();

        if (selectedNodes.Count == 0)
            return;

        // 古い位置を保存
        var oldPositions = selectedNodes.Select(n => (n, n.X, n.Y)).ToList();

        // ノードを階層ごとに分類（トポロジカルソート）
        var layers = ComputeNodeLayers(selectedNodes);

        // 各階層の配置パラメータ
        const double horizontalSpacing = 100.0; // 階層間の最小水平間隔
        const double verticalSpacing = 20.0;    // 同一階層内のノード間の最小垂直間隔

        // 最初のノードの位置を基準点とする
        var baseX = selectedNodes.Min(n => n.X);
        var baseY = selectedNodes.Min(n => n.Y);

        // ノードサイズのキャッシュを作成
        var nodeSizes = new Dictionary<EditorNode, (double width, double height)>();
        foreach (var node in selectedNodes)
        {
            var nodeControl = FindNodeControl(node);
            if (nodeControl != null)
            {
                var width = nodeControl.Bounds.Width > 0 ? nodeControl.Bounds.Width : 200.0;
                var height = nodeControl.Bounds.Height > 0 ? nodeControl.Bounds.Height : 100.0;
                nodeSizes[node] = (width, height);
            }
            else
            {
                nodeSizes[node] = (200.0, 100.0);
            }
        }

        // パス1: X座標を設定
        var currentX = baseX;

        foreach (var layer in layers)
        {
            var maxWidthInLayer = 0.0;
            foreach (var node in layer)
            {
                node.X = currentX;
                maxWidthInLayer = Math.Max(maxWidthInLayer, nodeSizes[node].width);
            }

            currentX += maxWidthInLayer + horizontalSpacing;
        }

        // パス2: Y座標を設定（前方から順番に確定していく）
        // 階層ごとに前から順番に処理することで、参照するノードは常に確定済み
        var nodesWithConnections = new HashSet<EditorNode>();
        var nodesWithoutConnections = new List<EditorNode>();

        foreach (var layer in layers)
        {
            foreach (var node in layer)
            {
                var nodeHeight = nodeSizes[node].height;

                // このノードに接続されている前の階層（すでに確定済み）のノードを取得
                var connectedPreviousNodes = Graph.Connections
                    .Where(c => c.TargetNode == node && selectedNodes.Contains(c.SourceNode))
                    .Select(c => c.SourceNode)
                    .Distinct()
                    .ToList();

                // このノードから接続されている次の階層のノードを取得
                var connectedNextNodes = Graph.Connections
                    .Where(c => c.SourceNode == node && selectedNodes.Contains(c.TargetNode))
                    .Select(c => c.TargetNode)
                    .Distinct()
                    .ToList();

                var hasConnections = connectedPreviousNodes.Count > 0 || connectedNextNodes.Count > 0;

                if (hasConnections)
                {
                    // 接続があるノード
                    nodesWithConnections.Add(node);

                    // 前方のノード（確定済み）の中心Y座標の平均を計算
                    if (connectedPreviousNodes.Count > 0)
                    {
                        var avgCenterY = connectedPreviousNodes.Average(n => n.Y + nodeSizes[n].height / 2.0);
                        node.Y = avgCenterY - nodeHeight / 2.0;
                    }
                    else
                    {
                        // 前方に接続がなく後方にのみ接続がある場合は基準Y座標
                        node.Y = baseY;
                    }
                }
                else
                {
                    // 接続がないノードは後で配置
                    nodesWithoutConnections.Add(node);
                }
            }

            // 同じ階層内で接続があるノードが重ならないように調整
            var sortedLayer = layer.Where(n => nodesWithConnections.Contains(n)).OrderBy(n => n.Y).ToList();
            for (var i = 1; i < sortedLayer.Count; i++)
            {
                var prevNode = sortedLayer[i - 1];
                var currNode = sortedLayer[i];
                var prevNodeBottom = prevNode.Y + nodeSizes[prevNode].height;
                var minY = prevNodeBottom + verticalSpacing;

                if (currNode.Y < minY)
                {
                    currNode.Y = minY;
                }
            }
        }

        // 接続がないノードを階層ごとに配置
        if (nodesWithoutConnections.Count > 0)
        {
            foreach (var layer in layers)
            {
                var disconnectedNodesInLayer = layer.Where(n => nodesWithoutConnections.Contains(n)).ToList();

                if (disconnectedNodesInLayer.Count == 0)
                    continue;

                // この階層の接続があるノードの最大Y座標を取得
                var connectedNodesInLayer = layer.Where(n => nodesWithConnections.Contains(n)).ToList();
                var currentY = connectedNodesInLayer.Count > 0
                    ? connectedNodesInLayer.Max(n => n.Y + nodeSizes[n].height) + verticalSpacing * 2
                    : baseY;

                // 接続がないノードを配置
                foreach (var node in disconnectedNodesInLayer)
                {
                    node.Y = currentY;
                    currentY += nodeSizes[node].height + verticalSpacing;
                }
            }
        }

        // 新しい位置と古い位置を組み合わせてアクションを作成
        var nodePositions = oldPositions
            .Select(old => (old.n, old.X, old.Y, old.n.X, old.n.Y))
            .ToList();

        var action = new ArrangeNodesAction(nodePositions);

        // 一度Undoして、Actionで再実行
        foreach (var (node, oldX, oldY, _, _) in nodePositions)
        {
            node.X = oldX;
            node.Y = oldY;
        }

        UndoRedoManager!.ExecuteAction(action);
        NotifyCanExecuteChanged();

        // コネクタの更新をスケジュール
        ScheduleConnectorUpdate();
    }

    /// <summary>
    /// ノードを階層に分類します（トポロジカルソートベース）
    /// </summary>
    private List<List<EditorNode>> ComputeNodeLayers(List<EditorNode> nodes)
    {
        if (Graph == null)
            return [];

        var layers = new List<List<EditorNode>>();
        var nodeToLayer = new Dictionary<EditorNode, int>();

        // 各ノードの入力依存関係を計算
        var dependencies = new Dictionary<EditorNode, List<EditorNode>>();
        foreach (var node in nodes)
        {
            dependencies[node] = [];
        }

        // 選択されたノード間の接続のみを考慮
        foreach (var connection in Graph.Connections)
        {
            if (nodes.Contains(connection.SourceNode) && nodes.Contains(connection.TargetNode))
            {
                dependencies[connection.TargetNode].Add(connection.SourceNode);
            }
        }

        // 深さ優先探索で各ノードの階層を決定
        void AssignLayer(EditorNode node, int layer)
        {
            if (nodeToLayer.TryGetValue(node, out var existingLayer))
            {
                // 既に割り当てられている場合は、より深い階層を優先
                if (layer > existingLayer)
                {
                    nodeToLayer[node] = layer;
                }
                return;
            }

            nodeToLayer[node] = layer;

            // このノードに依存しているノードは次の階層
            foreach (var connection in Graph.Connections)
            {
                if (connection.SourceNode == node && nodes.Contains(connection.TargetNode))
                {
                    AssignLayer(connection.TargetNode, layer + 1);
                }
            }
        }

        // 入力依存関係がないノード（ルートノード）から開始
        var rootNodes = nodes.Where(n => dependencies[n].Count == 0).ToList();

        // ルートノードがない場合（循環依存がある場合）は全ノードをルートとして扱う
        if (rootNodes.Count == 0)
        {
            rootNodes = nodes.ToList();
        }

        foreach (var rootNode in rootNodes)
        {
            AssignLayer(rootNode, 0);
        }

        // 階層ごとにノードをグループ化
        var maxLayer = nodeToLayer.Values.DefaultIfEmpty(-1).Max();
        for (var i = 0; i <= maxLayer; i++)
        {
            layers.Add([]);
        }

        foreach (var (node, layer) in nodeToLayer)
        {
            layers[layer].Add(node);
        }

        // 階層に属していないノード（孤立ノード）を最初の階層に追加
        var unassignedNodes = nodes.Where(n => !nodeToLayer.ContainsKey(n)).ToList();
        if (unassignedNodes.Count > 0)
        {
            if (layers.Count == 0)
            {
                layers.Add([]);
            }
            layers[0].AddRange(unassignedNodes);
        }

        return layers;
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

        // Undo/Redo対応で接続を削除
        foreach (var connection in selectedConnections)
        {
            var action = new DeleteConnectionAction(Graph, connection);
            UndoRedoManager!.ExecuteAction(action);
        }

        NotifyCanExecuteChanged();
    }

    #endregion

    #region Copy/Paste/Cut

    /// <summary>
    /// 選択されたノードをコピーします
    /// </summary>
    private void CopySelectedNodes()
    {
        if (Graph == null)
            return;

        var selectedNodes = Graph.SelectionManager.SelectedItems
            .OfType<EditorNode>()
            .ToArray();

        if (selectedNodes.Length == 0)
            return;

        if (VisualRoot is Window window)
        {
            // 選択されたノードをCloneして、グラフとレイアウトをシリアライズ
            var clonedGraph = Graph.Clone(selectedNodes);
            var graphYaml = GraphSerializer.Serialize(clonedGraph.Graph);
            var layoutYaml = EditorLayoutSerializer.SaveLayout(clonedGraph);

            // グラフとレイアウトを結合してクリップボードに保存
            var combined = $"---GRAPH---\n{graphYaml}\n---LAYOUT---\n{layoutYaml}";
            window.Clipboard?.SetTextAsync(combined);
        }
    }

    /// <summary>
    /// コピーしたノードをペーストします
    /// </summary>
    private async Task PasteNodes()
    {
        if (Graph == null)
            return;

        if (VisualRoot is Window { Clipboard: not null } window)
        {
            var clipBoard = await window.Clipboard.TryGetTextAsync();
            if (string.IsNullOrEmpty(clipBoard))
            {
                return;
            }

            try
            {
                // クリップボードからグラフとレイアウトを分離
                var parts = clipBoard.Split(["---GRAPH---", "---LAYOUT---"], StringSplitOptions.None);
                if (parts.Length != 3)
                {
                    return;
                }

                var graphYaml = parts[1];
                var layoutYaml = parts[2];

                // グラフをデシリアライズ
                var pastedGraph = GraphSerializer.Deserialize(graphYaml);

                // EditorNodeに変換
                var editorNodes = pastedGraph.Nodes.Select(n => new EditorNode(Graph.SelectionManager, n)).ToArray();
                
                // グラフに追加
                Graph.AddNode(editorNodes);
                
                // レイアウトを適用
                EditorLayoutSerializer.LoadLayout(layoutYaml, Graph);
                // 少しオフセットして配置
                foreach (var editorNode in editorNodes)
                {
                    editorNode.X += 30;
                    editorNode.Y += 30;
                }

                // 選択をクリアして、ペーストしたノードを選択
                Graph.SelectionManager.ClearSelection();
                foreach (var editorNode in editorNodes)
                {
                    Graph.SelectionManager.AddToSelection(editorNode);
                }

                OnGraphChanged();
            }
            catch
            {
                // デシリアライズ失敗時は何もしない
            }
        }
    }

    #endregion

    #region Node Duplication

    /// <summary>
    /// ノードを複製し、ノード間の接続も再現します（Undo/Redo用）
    /// </summary>
    public (List<EditorNode> Nodes, List<EditorConnection> Connections) DuplicateNodesWithConnectionsForUndo(List<EditorNode> nodesToDuplicate)
    {
        if (Graph == null || nodesToDuplicate.Count == 0)
            return ([], []);

        var newNodes = new List<EditorNode>();
        var newConnections = new List<EditorConnection>();
        var oldToNewNodeMap = new Dictionary<EditorNode, EditorNode>();

        // すべてのノードを複製（Graphには追加しない）
        foreach (var editorNode in nodesToDuplicate)
        {
            var newNode = editorNode.Clone();
            newNodes.Add(newNode);
            oldToNewNodeMap[editorNode] = newNode;
        }

        // コピー元のノード間の接続を再現
        foreach (var oldNode in nodesToDuplicate)
        {
            if (!oldToNewNodeMap.TryGetValue(oldNode, out var newNode))
                continue;

            // 出力ポートから接続されている入力ポートを確認
            for (var outputIndex = 0; outputIndex < oldNode.OutputPorts.Count; outputIndex++)
            {
                var oldOutputPort = oldNode.OutputPorts[outputIndex];
                var outputPort = oldOutputPort.Port as OutputPort;
                if (outputPort == null)
                    continue;

                // この出力ポートに接続されているすべての入力ポートを確認
                foreach (var connectedInputPort in outputPort.ConnectedPorts)
                {
                    // 接続先のノードがコピー対象に含まれているか確認
                    var targetOldNode = nodesToDuplicate.FirstOrDefault(n =>
                        n.InputPorts.Any(p => p.Port == connectedInputPort));

                    if (targetOldNode != null && oldToNewNodeMap.TryGetValue(targetOldNode, out var targetNewNode))
                    {
                        // 対応する入力ポートのインデックスを見つける
                        var inputIndex = targetOldNode.InputPorts
                            .Select((p, i) => new { Port = p, Index = i })
                            .FirstOrDefault(x => x.Port.Port == connectedInputPort)?.Index;

                        if (inputIndex.HasValue)
                        {
                            // EditorConnectionを作成（まだモデルレベルでは接続しない）
                            var newOutputEditorPort = newNode.OutputPorts[outputIndex];
                            var newInputEditorPort = targetNewNode.InputPorts[inputIndex.Value];
                            var connection = new EditorConnection(newNode, newOutputEditorPort, targetNewNode, newInputEditorPort);
                            newConnections.Add(connection);
                        }
                    }
                }
            }
        }

        return (newNodes, newConnections);
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
        
        // SingleConnectPortの場合は既存接続を削除
        DisconnectIfSingleConnect(inputPort);
        DisconnectIfSingleConnect(outputPort);

        // Undo/Redo対応で接続を作成
        var action = new CreateConnectionAction(Graph, outputNode, outputPort, inputNode, inputPort);
        UndoRedoManager!.ExecuteAction(action);
        NotifyCanExecuteChanged();

        // 座標を更新
        ScheduleConnectorUpdate();

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
                            // Undo/Redo対応で削除
                            var deleteAction = new DeleteConnectionAction(Graph, oldConnection);
                            UndoRedoManager!.ExecuteAction(deleteAction);
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
        var nodeTypeService = app?.Services?.GetService<NodeTypeService>();

        if (nodeTypeService == null)
            return;

        // 親ウィンドウを取得
        if (this.GetVisualRoot() is not Window window)
            return;

        // GraphControl内の座標をウィンドウ座標に変換してからスクリーン座標へ変換
        var windowPosition = this.TranslatePoint(clickPosition, window);
        if (!windowPosition.HasValue)
            return;

        var screenPosition = window.PointToScreen(windowPosition.Value);

        // AddNodeWindowを表示
        var selectedNodeType = await AddNodeWindow.ShowDialog(screenPosition, nodeTypeService);

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

        // ノードを作成（まだGraphに追加しない）
        if (Activator.CreateInstance(nodeTypeInfo.NodeType) is not Node node)
            return;

        // EditorNodeを作成
        var editorNode = new EditorNode(Graph.SelectionManager, node)
        {
            X = position.X,
            Y = position.Y
        };

        // Undo/Redo対応でノードを追加
        var action = new AddNodeAction(Graph, editorNode);
        UndoRedoManager!.ExecuteAction(action);
        NotifyCanExecuteChanged();

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

    private void NotifyCanExecuteChanged()
    {
        // MainWindowViewModelのCanUndo/CanRedoを更新
        if (DataContext is ViewModels.MainWindowViewModel viewModel)
        {
            viewModel.NotifyUndoRedoCanExecuteChanged();
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
