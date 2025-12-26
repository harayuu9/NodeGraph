using System;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Primitives;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlの入力イベント処理部分
/// ポインターイベント、キーボードイベントなど
/// </summary>
public partial class GraphControl
{
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled || Graph == null) return;

        // フォーカスを取得（Deleteキーなどを受け取るため）
        Focus();

        var properties = e.GetCurrentPoint(this).Properties;

        // 右ボタンの処理（背景クリック時のAddNodeWindow表示用）
        // 読み取り専用モードでは無効
        if (properties.IsRightButtonPressed && !Graph.IsExecuting && !IsReadOnly)
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

        // 中ボタンでドラッグ開始（読み取り専用でも許可）
        if (properties.IsMiddleButtonPressed)
        {
            _isDragging = true;
            _lastDragPoint = e.GetPosition(this);
            e.Pointer.Capture(this);
            e.Handled = true;

            OnPanStarted();
        }

        // 左ボタンでの選択操作（読み取り専用モードでは無効）
        if (properties.IsLeftButtonPressed && !Graph.IsExecuting && !IsReadOnly)
            // NodeControl上またはポートドラッグ中でのクリックでない場合
            if (e.Source is not NodeControl && !_isDraggingPort)
            {
                // コネクタの近くをクリックしたかチェック（遊びを持たせる）
                // ズームに関わらず画面上で一定のクリックしやすさを提供するため、判定範囲をズームで調整
                var tolerance = 8.0 / Zoom;
                var hitConnector = FindConnectorAt(e.GetPosition(this), tolerance);

                if (hitConnector?.Connection != null)
                {
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                        Graph.SelectionManager.ToggleSelection(hitConnector.Connection);
                    else
                        Graph.SelectionManager.Select(hitConnector.Connection);

                    e.Handled = true;
                    return;
                }

                // コネクタもヒットしなかった場合は矩形選択を開始
                _isSelecting = true;
                _selectionStartPoint = e.GetPosition(this);

                IsSelectionVisible = true;
                SelectionRect = new Rect(_selectionStartPoint, new Size(0, 0));

                e.Pointer.Capture(this);
                e.Handled = true;

                // Ctrlキーが押されていない場合は選択をクリア
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) Graph?.SelectionManager.ClearSelection();
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
                // Output portからドラッグしている場合、終点を更新
                DragConnectorLine = new ConnectorLine(DragConnectorLine.Start, currentPoint);
            else
                // Input portからドラッグしている場合、始点を更新
                DragConnectorLine = new ConnectorLine(currentPoint, DragConnectorLine.End);

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
        if (Graph == null) return;

        // 読み取り専用モードでは編集操作を無効化
        if (IsReadOnly) return;

        if (e.Key == Key.Delete)
        {
            DeleteSelectedItems();
            e.Handled = true;
        }

        if (e.Key == Key.R)
        {
            ArrangeSelectedNodes();
            e.Handled = true;
        }

        if (e.KeyModifiers == KeyModifiers.Control)
            switch (e.Key)
            {
                case Key.C:
                    CopySelectedNodes();
                    e.Handled = true;
                    break;
                case Key.V:
                    _ = PasteNodes();
                    e.Handled = true;
                    break;
                case Key.D:
                    DuplicateSelectedNodes(Graph.SelectionManager.SelectedItems
                        .OfType<EditorNode>()
                        .ToList());
                    e.Handled = true;
                    break;
                case Key.Z:
                    UndoRedoManager?.Undo();
                    e.Handled = true;
                    break;
                case Key.Y:
                    UndoRedoManager?.Redo();
                    e.Handled = true;
                    break;
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

    private void UpdateSelectionRectangle(Point start, Point end)
    {
        var topLeft = new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
        var bottomRight = new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
        SelectionRect = new Rect(topLeft, bottomRight);
    }
}