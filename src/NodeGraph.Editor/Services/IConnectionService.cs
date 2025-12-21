using System.Collections.Generic;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Undo;

namespace NodeGraph.Editor.Services;

/// <summary>
/// 接続管理を提供するサービスのインターフェース
/// 接続の作成・削除など
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// 接続を作成し、Undo/Redo可能なアクションを返します
    /// </summary>
    /// <param name="graph">対象のグラフ</param>
    /// <param name="sourcePort">接続元ポート</param>
    /// <param name="targetPort">接続先ポート</param>
    /// <param name="undoRedoManager">Undo/Redoマネージャー</param>
    /// <returns>接続が作成された場合はtrue</returns>
    bool CreateConnection(
        EditorGraph graph,
        EditorPort sourcePort,
        EditorPort targetPort,
        UndoRedoManager undoRedoManager);

    /// <summary>
    /// 2つのポートが接続可能かどうかを判定します
    /// </summary>
    bool CanConnect(EditorPort sourcePort, EditorPort targetPort);

    /// <summary>
    /// 選択された接続を削除します
    /// </summary>
    /// <param name="graph">対象のグラフ</param>
    /// <param name="connections">削除する接続のリスト</param>
    /// <param name="undoRedoManager">Undo/Redoマネージャー</param>
    void DeleteConnections(
        EditorGraph graph,
        IEnumerable<EditorConnection> connections,
        UndoRedoManager undoRedoManager);
}