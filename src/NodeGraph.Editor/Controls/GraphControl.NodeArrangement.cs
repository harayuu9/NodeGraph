using System.Linq;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Services;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlのノード配置機能
/// ノードの自動整列（TopologicalLayoutServiceに委譲）
/// </summary>
public partial class GraphControl
{
    private IGraphLayoutService? _layoutService;

    /// <summary>
    /// レイアウトサービスを取得または作成します
    /// </summary>
    private IGraphLayoutService GetLayoutService()
    {
        return _layoutService ??= new TopologicalLayoutService();
    }

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

        // ノードサイズ取得関数
        (double width, double height) GetNodeSize(EditorNode node)
        {
            var nodeControl = FindNodeControl(node);
            if (nodeControl != null)
            {
                var width = nodeControl.Bounds.Width > 0 ? nodeControl.Bounds.Width : 200.0;
                var height = nodeControl.Bounds.Height > 0 ? nodeControl.Bounds.Height : 100.0;
                return (width, height);
            }

            return (200.0, 100.0);
        }

        // レイアウトサービスを使用してアクションを作成
        var layoutService = GetLayoutService();
        var action = layoutService.CreateArrangeAction(selectedNodes, Graph, GetNodeSize);

        // Undo/Redo対応で実行
        UndoRedoManager!.ExecuteAction(action);
        NotifyCanExecuteChanged();

        // コネクタの更新をスケジュール
        ScheduleConnectorUpdate();
    }
}