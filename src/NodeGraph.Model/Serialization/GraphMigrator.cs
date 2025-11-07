namespace NodeGraph.Model.Serialization;

/// <summary>
/// グラフファイルのバージョンマイグレーションを管理します
/// </summary>
public static class GraphMigrator
{
    private static readonly Dictionary<string, Func<GraphData, GraphData>> Migrations = new();

    static GraphMigrator()
    {
        // 将来的なマイグレーション登録例:
        // RegisterMigration("1.0.0", "1.1.0", Migrate_1_0_to_1_1);
        // RegisterMigration("1.1.0", "2.0.0", Migrate_1_1_to_2_0);
    }

    /// <summary>
    /// マイグレーション処理を登録します
    /// </summary>
    /// <param name="fromVersion">移行元バージョン</param>
    /// <param name="toVersion">移行先バージョン</param>
    /// <param name="migration">マイグレーション関数</param>
    public static void RegisterMigration(string fromVersion, string toVersion,
        Func<GraphData, GraphData> migration)
    {
        var key = $"{fromVersion}->{toVersion}";
        Migrations[key] = migration;
    }

    /// <summary>
    /// GraphDataを現在のバージョンにマイグレーションします
    /// </summary>
    /// <param name="data">マイグレーション対象のデータ</param>
    /// <param name="targetVersion">目標バージョン</param>
    /// <returns>マイグレーション後のデータ</returns>
    public static GraphData Migrate(GraphData data, string targetVersion)
    {
        if (!Version.TryParse(data.Version, out var currentVersion) ||
            !Version.TryParse(targetVersion, out var target))
        {
            throw new ArgumentException("Invalid version format");
        }

        // すでに目標バージョンの場合は何もしない
        if (currentVersion >= target)
        {
            return data;
        }

        // マイグレーションチェーンを構築
        var migrationChain = BuildMigrationChain(data.Version, targetVersion);

        // 各マイグレーションを順次適用
        var current = data;
        foreach (var migration in migrationChain)
        {
            current = migration(current);
        }

        return current;
    }

    /// <summary>
    /// 開始バージョンから目標バージョンへのマイグレーションチェーンを構築します
    /// </summary>
    private static List<Func<GraphData, GraphData>> BuildMigrationChain(
        string fromVersion, string toVersion)
    {
        var chain = new List<Func<GraphData, GraphData>>();

        // TODO: 実際のマイグレーションパスを検索するロジックを実装
        // 現時点では、直接のマイグレーションのみをサポート
        var key = $"{fromVersion}->{toVersion}";
        if (Migrations.TryGetValue(key, out var migration))
        {
            chain.Add(migration);
        }

        return chain;
    }

    // 将来的なマイグレーション例:

    // /// <summary>
    // /// 1.0.0から1.1.0へのマイグレーション
    // /// 例: 新しいプロパティの追加、古いプロパティのデフォルト値設定など
    // /// </summary>
    // private static GraphData Migrate_1_0_to_1_1(GraphData data)
    // {
    //     // 各ノードに新しいプロパティを追加
    //     foreach (var node in data.Nodes)
    //     {
    //         if (!node.Properties.ContainsKey("newProperty"))
    //         {
    //             node.Properties["newProperty"] = "defaultValue";
    //         }
    //     }
    //
    //     data.Version = "1.1.0";
    //     return data;
    // }

    // /// <summary>
    // /// 1.1.0から2.0.0へのマイグレーション
    // /// 例: 型名の変更、構造の変更など
    // /// </summary>
    // private static GraphData Migrate_1_1_to_2_0(GraphData data)
    // {
    //     // ノード型名を更新
    //     foreach (var node in data.Nodes)
    //     {
    //         if (node.Type == "OldNodeName")
    //         {
    //             node.Type = "NewNodeName";
    //         }
    //     }
    //
    //     data.Version = "2.0.0";
    //     return data;
    // }
}
