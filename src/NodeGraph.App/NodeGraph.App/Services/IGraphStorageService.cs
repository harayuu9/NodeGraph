using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NodeGraph.App.Models;
using NodeGraph.App.Selection;

namespace NodeGraph.App.Services;

/// <summary>
/// グラフの保存先メタデータ
/// </summary>
/// <param name="Id">一意のID</param>
/// <param name="Name">表示名</param>
/// <param name="CreatedAt">作成日時</param>
/// <param name="ModifiedAt">最終更新日時</param>
public record GraphMetadata(
    string Id,
    string Name,
    DateTime CreatedAt,
    DateTime ModifiedAt
);

/// <summary>
/// グラフの保存・読み込みを抽象化するサービスインターフェース。
/// プラットフォーム別の実装（ファイルシステム、localStorage等）を提供する。
/// </summary>
public interface IGraphStorageService
{
    /// <summary>
    /// 保存済みグラフの一覧を取得します。
    /// </summary>
    Task<IReadOnlyList<GraphMetadata>> GetSavedGraphsAsync();

    /// <summary>
    /// グラフを保存します。
    /// </summary>
    /// <param name="graph">保存するグラフ</param>
    /// <param name="name">グラフの表示名</param>
    /// <param name="existingId">既存グラフを上書きする場合はそのID</param>
    /// <returns>保存されたグラフのID</returns>
    Task<string> SaveGraphAsync(EditorGraph graph, string name, string? existingId = null);

    /// <summary>
    /// グラフを読み込みます。
    /// </summary>
    /// <param name="graphId">読み込むグラフのID</param>
    /// <param name="selectionManager">選択マネージャー</param>
    /// <returns>読み込まれたグラフ、または存在しない場合はnull</returns>
    Task<EditorGraph?> LoadGraphAsync(string graphId, SelectionManager selectionManager);

    /// <summary>
    /// グラフを削除します。
    /// </summary>
    /// <param name="graphId">削除するグラフのID</param>
    /// <returns>削除に成功した場合はtrue</returns>
    Task<bool> DeleteGraphAsync(string graphId);

    /// <summary>
    /// グラフの名前を変更します。
    /// </summary>
    /// <param name="graphId">対象グラフのID</param>
    /// <param name="newName">新しい名前</param>
    /// <returns>名前変更に成功した場合はtrue</returns>
    Task<bool> RenameGraphAsync(string graphId, string newName);
}
