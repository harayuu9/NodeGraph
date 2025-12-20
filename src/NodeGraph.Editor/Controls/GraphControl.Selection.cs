using System;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using NodeGraph.Editor.Selection;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlの選択ロジック部分
/// 矩形選択によるノード・接続の選択処理
/// </summary>
public partial class GraphControl
{
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
            .Cast<ISelectable>();

        // 矩形内の接続を検出
        var selectedConnections = GetAllConnectorControls()
            .Where(connector => connector.Intersects(selectionRect))
            .Where(connector => connector.Connection != null)
            .Select(connector => connector.Connection!)
            .Cast<ISelectable>();

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

    /// <summary>
    /// 指定された座標（ビューポート座標）の近くにあるConnectorControlを検索します
    /// </summary>
    private ConnectorControl? FindConnectorAt(Point point, double tolerance)
    {
        if (Graph == null || _canvas == null)
            return null;

        // ビューポート座標をキャンバス座標に変換
        var canvasPoint = this.TranslatePoint(point, _canvas);
        if (!canvasPoint.HasValue)
            return null;

        // 遊び（tolerance）を持たせた判定矩形を作成
        var hitRect = new Rect(
            canvasPoint.Value.X - tolerance,
            canvasPoint.Value.Y - tolerance,
            tolerance * 2,
            tolerance * 2);

        // 矩形と交差するコネクタを検索
        return GetAllConnectorControls()
            .FirstOrDefault(connector => connector.Intersects(hitRect));
    }
}
