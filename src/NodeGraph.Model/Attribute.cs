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