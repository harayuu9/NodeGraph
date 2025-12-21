namespace NodeGraph.Model;

/// <summary>
/// ノード定義属性。全てのノードに適用します。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class NodeAttribute(string? displayName = null, string? directory = null, params string[] execOutNames) : Attribute
{
    public string? DisplayName => displayName;
    public string? Directory => directory;
    /// <summary>
    /// 実行フロー出力ポートの名前。指定しない場合は["Out"]がデフォルト。
    /// HasExecOut=falseの場合は無視されます。
    /// </summary>
    public string[] ExecOutNames => execOutNames.Length > 0 ? execOutNames : ["Out"];
    /// <summary>
    /// 実行フロー入力ポートを持つかどうか。falseの場合はエントリーポイントノード。
    /// </summary>
    public bool HasExecIn { get; set; } = true;
    /// <summary>
    /// 実行フロー出力ポートを持つかどうか。falseの場合は定数/データノード。
    /// </summary>
    public bool HasExecOut { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Field)]
public class InputAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public class OutputAttribute : Attribute;

/// <summary>
/// クラスに対してJSON関連ノード（Deserialize, Serialize, Schema）を自動生成します。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class JsonNodeAttribute : Attribute
{
    /// <summary>
    /// 生成されるノードの表示名プレフィックス。指定しない場合はクラス名を使用。
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// ノードブラウザでのカテゴリ/ディレクトリ。デフォルトは"Json"。
    /// </summary>
    public string Directory { get; set; } = "Json";

    /// <summary>
    /// スキーマで"additionalProperties": falseを設定するか。デフォルトはtrue。
    /// </summary>
    public bool StrictSchema { get; set; } = true;
}

/// <summary>
/// JSONプロパティのシリアライズ動作をカスタマイズします。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class JsonPropertyAttribute : Attribute
{
    /// <summary>
    /// JSONプロパティ名。指定しない場合はプロパティ名をcamelCaseに変換。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// JSONスキーマのdescriptionフィールド。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// プロパティが必須かどうか。デフォルトはtrue（non-nullable型の場合）。
    /// </summary>
    public bool Required { get; set; } = true;
}