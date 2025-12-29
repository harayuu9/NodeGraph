using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Primitives;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Services;
using NodeGraph.Editor.Undo;
using NodeGraph.Editor.ViewModels;
using NodeGraph.Editor.Views;
using NodeGraph.Model;
using SelectionChangedEventArgs = NodeGraph.Editor.Selection.SelectionChangedEventArgs;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlはEditorGraphを視覚化し、パンとズーム機能を提供します
/// </summary>
public partial class GraphControl : TemplatedControl
{
    private Canvas? _canvas;
    private GridDecorator? _gridDecorator;
    private Canvas? _overlayCanvas;
    private Border? _touchGuard;
    private Canvas? _uiCanvas;

    // ノード追加パネル（オーバーレイ）用
    private Canvas? _addNodeOverlay;
    private TextBox? _addNodeSearchBox;
    private TreeView? _addNodeTreeView;
    private Point _addNodeClickPosition;
    private AddNodeWindowViewModel? _addNodeViewModel;

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

        // ノード追加パネル（オーバーレイ）
        _addNodeOverlay = e.NameScope.Find<Canvas>("PART_AddNodeOverlay");
        _addNodeSearchBox = e.NameScope.Find<TextBox>("PART_AddNodeSearchBox");
        _addNodeTreeView = e.NameScope.Find<TreeView>("PART_AddNodeTreeView");
        var addNodeOverlayBackground = e.NameScope.Find<Border>("PART_AddNodeOverlayBackground");
        if (addNodeOverlayBackground != null)
        {
            addNodeOverlayBackground.PointerPressed += OnAddNodeOverlayBackgroundPressed;
        }

        // オーバーレイのイベントを設定
        SetupAddNodeOverlay();

        if (_canvas != null)
        {
            _canvas.RenderTransform = _transformGroup;
            OnGraphChanged();
        }

        // オーバーレイキャンバスにも同じトランスフォームを適用（コネクタ用）
        if (_overlayCanvas != null) _overlayCanvas.RenderTransform = _transformGroup;

        // グリッドは Render 時に Pan/Zoom を考慮して描画するため、
        // RenderTransform は適用しない。
        if (_gridDecorator != null) UpdateGrid();

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

    #region Connection Management

    /// <summary>
    /// 選択されているすべてのアイテム（ノードおよび接続）を削除します
    /// </summary>
    public void DeleteSelectedItems()
    {
        if (Graph == null || UndoRedoManager == null)
            return;

        var selectedItems = Graph.SelectionManager.SelectedItems.ToList();
        if (selectedItems.Count == 0)
            return;

        UndoRedoManager.BeginTransaction();
        try
        {
            // 1. 接続を削除
            var selectedConnections = selectedItems.OfType<EditorConnection>().ToList();
            if (selectedConnections.Count > 0)
            {
                var connectionService = GetConnectionService();
                connectionService.DeleteConnections(Graph, selectedConnections, UndoRedoManager);
            }

            // 2. ノードを削除
            var selectedNodes = selectedItems.OfType<EditorNode>().ToList();
            foreach (var node in selectedNodes)
            {
                var action = new DeleteNodeAction(Graph, node);
                UndoRedoManager.ExecuteAction(action);
            }

            Graph.SelectionManager.ClearSelection();
            NotifyCanExecuteChanged();
        }
        finally
        {
            UndoRedoManager.EndTransaction();
        }

        if (DataContext is MainWindowViewModel viewModel) viewModel.NotifyUndoRedoCanExecuteChanged();
    }

    #endregion

    #region Node Duplication

    /// <summary>
    /// 選択されたノードを複製します（Copy→Pasteと同じロジック）
    /// </summary>
    /// <param name="nodesToDuplicate">複製するノード</param>
    /// <returns>複製されたノードの配列</returns>
    public EditorNode[]? DuplicateSelectedNodes(List<EditorNode> nodesToDuplicate)
    {
        if (Graph == null || nodesToDuplicate.Count == 0)
            return null;

        // ClipboardServiceを使ってシリアライズ→デシリアライズで複製
        var clipboardService = GetClipboardService();
        var clipboardData = clipboardService.SerializeNodes(nodesToDuplicate.ToArray(), Graph);
        var duplicatedNodes = clipboardService.DeserializeNodes(clipboardData, Graph);

        if (duplicatedNodes == null || duplicatedNodes.Length == 0)
            return null;

        // Undo/Redo対応でノードを追加
        var action = new AddNodesAction(Graph, duplicatedNodes);
        UndoRedoManager!.ExecuteAction(action);

        // 選択をクリアして、複製したノードを選択
        Graph.SelectionManager.ClearSelection();
        foreach (var node in duplicatedNodes) Graph.SelectionManager.AddToSelection(node);

        OnGraphChanged();

        return duplicatedNodes;
    }

    #endregion

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

    public static readonly StyledProperty<Vector> PanOffsetProperty = AvaloniaProperty.Register<GraphControl, Vector>(nameof(PanOffset));

    public Vector PanOffset
    {
        get => GetValue(PanOffsetProperty);
        set => SetValue(PanOffsetProperty, value);
    }

    public static readonly StyledProperty<bool> IsSelectionVisibleProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(IsSelectionVisible));

    public bool IsSelectionVisible
    {
        get => GetValue(IsSelectionVisibleProperty);
        set => SetValue(IsSelectionVisibleProperty, value);
    }

    public static readonly StyledProperty<Rect> SelectionRectProperty = AvaloniaProperty.Register<GraphControl, Rect>(nameof(SelectionRect));

    public Rect SelectionRect
    {
        get => GetValue(SelectionRectProperty);
        set => SetValue(SelectionRectProperty, value);
    }

    public static readonly StyledProperty<bool> IsDraggingConnectorProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(IsDraggingConnector));

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

    public static readonly StyledProperty<string?> DragConnectorPortTypeProperty = AvaloniaProperty.Register<GraphControl, string?>(nameof(DragConnectorPortType));

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

    public static readonly StyledProperty<bool> IsInputBlockedProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(IsInputBlocked));

    public bool IsInputBlocked
    {
        get => GetValue(IsInputBlockedProperty);
        set => SetValue(IsInputBlockedProperty, value);
    }

    public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(IsReadOnly));

    /// <summary>
    /// 読み取り専用モード。パン/ズームは可能だが、編集操作（ノード追加、選択、接続など）は無効になります。
    /// </summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public static readonly StyledProperty<bool> IsAddNodePanelVisibleProperty = AvaloniaProperty.Register<GraphControl, bool>(nameof(IsAddNodePanelVisible));

    /// <summary>
    /// ノード追加パネル（オーバーレイ）の表示状態
    /// </summary>
    public bool IsAddNodePanelVisible
    {
        get => GetValue(IsAddNodePanelVisibleProperty);
        set => SetValue(IsAddNodePanelVisibleProperty, value);
    }

    public static readonly StyledProperty<Point> AddNodePanelPositionProperty = AvaloniaProperty.Register<GraphControl, Point>(nameof(AddNodePanelPosition));

    /// <summary>
    /// ノード追加パネルの表示位置
    /// </summary>
    public Point AddNodePanelPosition
    {
        get => GetValue(AddNodePanelPositionProperty);
        set => SetValue(AddNodePanelPositionProperty, value);
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
        foreach (var node in Graph.Nodes) node.PropertyChanged -= OnNodePropertyChanged;

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
        if (e.PropertyName == nameof(EditorGraph.IsExecuting)) IsInputBlocked = Graph?.IsExecuting ?? false;
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorNode.X) or nameof(EditorNode.Y)) ScheduleConnectorUpdate();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // ConnectorControlの選択状態を更新
        var connectors = GetAllConnectorControls();
        foreach (var connector in connectors)
            if (connector.Connection != null)
                connector.IsSelected = Graph?.SelectionManager.IsSelected(connector.Connection) ?? false;
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
                if (nodeControl != null) _canvas.Children.Remove(nodeControl);
            }

            // コネクタを更新
            ScheduleConnectorUpdate();
        }
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

    #region Port Drag Handling

    private void OnPortDragStarted(object? sender, RoutedEventArgs e)
    {
        if (e is not PortDragEventArgs args || _canvas == null)
            return;

        // 読み取り専用モードではポート接続を無効化
        if (IsReadOnly)
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
            // 接続を作成
            CreateConnection(sourcePort, targetPort);

        CleanupPortDrag();
    }

    private void UpdatePortHighlight(Point mousePosition)
    {
        if (_dragSourcePort?.Port == null)
            return;

        // マウス位置にあるPortControlを検索
        var portControl = GetPortAtPosition(mousePosition);

        // 前回ハイライトしていたポートをクリア
        if (_currentHoverPort != null && _currentHoverPort != portControl) _currentHoverPort.IsHighlighted = false;

        _currentHoverPort = null;

        // 新しいポートをハイライト（接続可能な場合のみ）
        if (portControl != null && portControl != _dragSourcePort && portControl.Port != null)
            if (CanConnect(_dragSourcePort.Port, portControl.Port))
            {
                _currentHoverPort = portControl;
                _currentHoverPort.IsHighlighted = true;
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
        var connectionService = GetConnectionService();
        return connectionService.CanConnect(sourcePort, targetPort);
    }

    private IConnectionService? _connectionService;

    private IConnectionService GetConnectionService()
    {
        return _connectionService ??= new ConnectionService();
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


    private void CreateConnection(EditorPort sourcePort, EditorPort targetPort)
    {
        if (Graph == null || UndoRedoManager == null)
            return;

        var connectionService = GetConnectionService();
        if (connectionService.CreateConnection(Graph, sourcePort, targetPort, UndoRedoManager))
        {
            NotifyCanExecuteChanged();
            ScheduleConnectorUpdate();
        }
    }

    #endregion

    #region Add Node

    private void SetupAddNodeOverlay()
    {
        if (_addNodeSearchBox != null)
        {
            _addNodeSearchBox.TextChanged += OnAddNodeSearchTextChanged;
            _addNodeSearchBox.KeyDown += OnAddNodeSearchKeyDown;
        }

        if (_addNodeTreeView != null)
        {
            _addNodeTreeView.DoubleTapped += OnAddNodeTreeViewDoubleTapped;
            _addNodeTreeView.KeyDown += OnAddNodeTreeViewKeyDown;
        }
    }

    private void OnAddNodeSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_addNodeViewModel != null && _addNodeSearchBox != null)
        {
            _addNodeViewModel.SearchText = _addNodeSearchBox.Text ?? string.Empty;
        }
    }

    private void OnAddNodeSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideAddNodeOverlay();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ConfirmAddNodeSelection();
            e.Handled = true;
        }
        else if (e.Key == Key.Down && _addNodeTreeView != null)
        {
            _addNodeTreeView.Focus();
            e.Handled = true;
        }
    }

    private void OnAddNodeTreeViewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideAddNodeOverlay();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ConfirmAddNodeSelection();
            e.Handled = true;
        }
    }

    private void OnAddNodeTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        // ダブルタップされた要素からNodeTreeItemを取得
        if (e.Source is Control control)
        {
            var item = control.DataContext as NodeTreeItem;
            // 親要素をたどってNodeTreeItemを探す
            var current = control;
            while (item == null && current != null)
            {
                item = current.DataContext as NodeTreeItem;
                current = current.Parent as Control;
            }

            if (item?.NodeTypeInfo != null)
            {
                ConfirmAddNodeSelection(item);
                return;
            }
        }

        // フォールバック: TreeViewの選択アイテムを使用
        if (_addNodeTreeView?.SelectedItem is NodeTreeItem selectedItem)
        {
            ConfirmAddNodeSelection(selectedItem);
        }
    }

    private void ConfirmAddNodeSelection(NodeTreeItem? item = null)
    {
        // 引数が無い場合はTreeViewの選択アイテムを使用
        item ??= _addNodeTreeView?.SelectedItem as NodeTreeItem;

        if (item?.NodeTypeInfo != null)
        {
            var selectedNodeType = item.NodeTypeInfo;
            HideAddNodeOverlay();

            // キャンバス座標に変換してノードを作成
            if (_canvas != null)
            {
                var canvasPosition = this.TranslatePoint(_addNodeClickPosition, _canvas);
                if (canvasPosition.HasValue)
                {
                    CreateNode(selectedNodeType, canvasPosition.Value);
                }
            }
        }
    }

    /// <summary>
    /// オーバーレイの背景がクリックされた時に閉じる
    /// </summary>
    private void OnAddNodeOverlayBackgroundPressed(object? sender, PointerPressedEventArgs e)
    {
        HideAddNodeOverlay();
        e.Handled = true;
    }

    private void ShowAddNodeOverlay(Point position, NodeTypeService nodeTypeService)
    {
        _addNodeClickPosition = position;
        _addNodeViewModel = new AddNodeWindowViewModel(nodeTypeService);

        if (_addNodeTreeView != null)
        {
            _addNodeTreeView.ItemsSource = _addNodeViewModel.TreeItems;
            _addNodeTreeView.Bind(TreeView.SelectedItemProperty,
                new Avalonia.Data.Binding(nameof(AddNodeWindowViewModel.SelectedItem))
                {
                    Source = _addNodeViewModel,
                    Mode = Avalonia.Data.BindingMode.TwoWay
                });
        }

        if (_addNodeSearchBox != null)
        {
            _addNodeSearchBox.Text = string.Empty;
        }

        // パネル位置を設定（画面からはみ出さないように調整）
        var panelWidth = 220.0;
        var panelHeight = 300.0;
        var x = Math.Min(position.X, Bounds.Width - panelWidth - 10);
        var y = Math.Min(position.Y, Bounds.Height - panelHeight - 10);
        AddNodePanelPosition = new Point(Math.Max(10, x), Math.Max(10, y));

        IsAddNodePanelVisible = true;

        // 検索ボックスにフォーカス
        Dispatcher.UIThread.Post(() => _addNodeSearchBox?.Focus(), DispatcherPriority.Input);
    }

    private void HideAddNodeOverlay()
    {
        IsAddNodePanelVisible = false;
        _addNodeViewModel = null;

        if (_addNodeTreeView != null)
        {
            _addNodeTreeView.ItemsSource = null;
        }
    }

    private async Task ShowAddNodeWindow(Point clickPosition)
    {
        if (Graph == null || _canvas == null)
            return;

        // NodeTypeServiceを取得（デスクトップはDIコンテナから、それ以外は直接作成）
        NodeTypeService? nodeTypeService = null;

        var app = Application.Current as App;
        if (app?.Services != null)
        {
            nodeTypeService = app.Services.GetService<NodeTypeService>();
        }

        // DIコンテナから取得できない場合は直接作成
        nodeTypeService ??= new NodeTypeService();

        // ブラウザ環境（SingleViewApplicationLifetime）ではオーバーレイを使用
        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime)
        {
            ShowAddNodeOverlay(clickPosition, nodeTypeService);
            return;
        }

        // デスクトップ環境では従来のWindow方式
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
            if (canvasPosition.HasValue) CreateNode(selectedNodeType, canvasPosition.Value);
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
        if (DataContext is MainWindowViewModel viewModel) viewModel.NotifyUndoRedoCanExecuteChanged();
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