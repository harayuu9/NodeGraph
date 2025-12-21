using System;
using System.Collections.Generic;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Undo;

namespace NodeGraph.Editor.Services;

/// <summary>
/// グラフのレイアウト機能を提供するサービスのインターフェース
/// ノードの自動配置、階層計算など
/// </summary>
public interface IGraphLayoutService
{
    /// <summary>
    /// ノードを自動整列し、Undo/Redo可能なアクションを作成します
    /// </summary>
    /// <param name="nodes">整列するノード</param>
    /// <param name="graph">対象のグラフ</param>
    /// <param name="getNodeSize">ノードのサイズを取得する関数</param>
    /// <returns>Undo/Redo可能な配置アクション</returns>
    MoveNodesAction CreateArrangeAction(
        IEnumerable<EditorNode> nodes,
        EditorGraph graph,
        Func<EditorNode, (double width, double height)> getNodeSize);
}