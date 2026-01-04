namespace NodeGraph.Api.DTOs;

/// <summary>
/// グラフメタデータDTO (一覧表示用)
/// </summary>
public record GraphMetadataDto(
    string Id,
    string Name,
    DateTime CreatedAt,
    DateTime ModifiedAt
);

/// <summary>
/// グラフデータDTO (詳細取得用)
/// </summary>
public record GraphDataDto(
    string Id,
    string Name,
    string GraphYaml,
    string LayoutYaml,
    DateTime CreatedAt,
    DateTime ModifiedAt
);

/// <summary>
/// グラフ保存リクエストDTO
/// </summary>
public record SaveGraphRequest(
    string Name,
    string GraphYaml,
    string LayoutYaml
);

/// <summary>
/// グラフ保存レスポンスDTO
/// </summary>
public record SaveGraphResponse(
    string Id,
    DateTime CreatedAt,
    DateTime ModifiedAt
);

/// <summary>
/// グラフ名前変更リクエストDTO
/// </summary>
public record RenameGraphRequest(
    string NewName
);
