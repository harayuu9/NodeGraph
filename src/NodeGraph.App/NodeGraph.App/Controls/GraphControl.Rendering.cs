using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace NodeGraph.App.Controls;

/// <summary>
/// GraphControlのレンダリング関連機能
/// コネクタの更新、グリッドの更新など
/// </summary>
public partial class GraphControl
{
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
    /// グリッドを更新します
    /// </summary>
    private void UpdateGrid()
    {
        // Decorator に再描画を依頼（プロパティはXAMLバインドで渡される）
        _gridDecorator?.InvalidateVisual();
    }
}