namespace NodeGraph.Editor.Selection;

/// <summary>
/// 選択可能なオブジェクトを表すインターフェース
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// このオブジェクトの一意な識別子
    /// </summary>
    object SelectionId { get; }
}
