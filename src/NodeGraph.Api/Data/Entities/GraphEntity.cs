using System.ComponentModel.DataAnnotations;

namespace NodeGraph.Api.Data.Entities;

/// <summary>
/// グラフデータのデータベースエンティティ
/// </summary>
public class GraphEntity
{
    /// <summary>
    /// グラフの一意識別子 (GUID)
    /// </summary>
    [Key]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// グラフの表示名
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// グラフ構造 (YAML形式)
    /// </summary>
    [Required]
    public string GraphYaml { get; set; } = string.Empty;

    /// <summary>
    /// レイアウト情報 (YAML形式)
    /// </summary>
    public string LayoutYaml { get; set; } = string.Empty;

    /// <summary>
    /// 作成日時 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新日時 (UTC)
    /// </summary>
    public DateTime ModifiedAt { get; set; }
}
