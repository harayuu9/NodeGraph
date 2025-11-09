using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Services;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlのクリップボード操作機能
/// コピー・ペースト（YamlClipboardServiceに委譲）
/// </summary>
public partial class GraphControl
{
    private IClipboardService? _clipboardService;

    /// <summary>
    /// クリップボードサービスを取得または作成します
    /// </summary>
    private IClipboardService GetClipboardService()
    {
        return _clipboardService ??= new YamlClipboardService();
    }

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
            var clipboardService = GetClipboardService();
            var clipboardData = clipboardService.SerializeNodes(selectedNodes, Graph);
            window.Clipboard?.SetTextAsync(clipboardData);
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

            var clipboardService = GetClipboardService();
            var editorNodes = clipboardService.DeserializeNodes(clipBoard, Graph);

            if (editorNodes == null || editorNodes.Length == 0)
            {
                return;
            }

            // Undo/Redo対応でノードを追加
            var action = new Undo.AddNodesAction(Graph, editorNodes);
            UndoRedoManager!.ExecuteAction(action);

            // 選択をクリアして、ペーストしたノードを選択
            Graph.SelectionManager.ClearSelection();
            foreach (var editorNode in editorNodes)
            {
                Graph.SelectionManager.AddToSelection(editorNode);
            }

            OnGraphChanged();

            if (DataContext is ViewModels.MainWindowViewModel viewModel)
            {
                viewModel.NotifyUndoRedoCanExecuteChanged();
            }
        }
    }
}
