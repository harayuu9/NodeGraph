using System.Threading.Tasks;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Services;

/// <summary>
/// クリップボード操作を提供するサービスのインターフェース
/// ノードのコピー・ペーストなど
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// ノードをシリアライズしてクリップボード形式の文字列に変換します
    /// </summary>
    /// <param name="nodes">シリアライズするノード</param>
    /// <param name="graph">対象のグラフ</param>
    /// <returns>クリップボード形式の文字列</returns>
    string SerializeNodes(EditorNode[] nodes, EditorGraph graph);

    /// <summary>
    /// クリップボードの文字列からノードをデシリアライズします
    /// </summary>
    /// <param name="clipboardData">クリップボードの文字列</param>
    /// <param name="targetGraph">ノードを追加する対象のグラフ</param>
    /// <returns>デシリアライズされたノードの配列。失敗時はnull</returns>
    EditorNode[]? DeserializeNodes(string clipboardData, EditorGraph targetGraph);
}
