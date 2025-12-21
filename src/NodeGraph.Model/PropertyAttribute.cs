namespace NodeGraph.Model;

/// <summary>
/// フィールドをノードの編集可能プロパティとしてマークします。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class PropertyAttribute : Attribute
{
    /// <summary>
    /// プロパティの表示名を取得または設定します。
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// プロパティのカテゴリを取得または設定します。
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// プロパティのツールチップテキストを取得または設定します。
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// プロパティをノード内に表示するかどうかを取得または設定します。
    /// </summary>
    public bool ShowInNode { get; set; } = true;

    /// <summary>
    /// プロパティをInspectorに表示するかどうかを取得または設定します。
    /// </summary>
    public bool ShowInInspector { get; set; } = false;
}

/// <summary>
/// 数値プロパティの最小値と最大値を指定します。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RangeAttribute : Attribute
{
    /// <summary>
    /// 範囲を指定してインスタンスを初期化します。
    /// </summary>
    /// <param name="min">最小値</param>
    /// <param name="max">最大値</param>
    public RangeAttribute(double min, double max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// 最小値を取得します。
    /// </summary>
    public double Min { get; }

    /// <summary>
    /// 最大値を取得します。
    /// </summary>
    public double Max { get; }
}

/// <summary>
/// 文字列プロパティを複数行入力可能にします。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class MultilineAttribute : Attribute
{
    /// <summary>
    /// 表示する行数を取得または設定します。
    /// </summary>
    public int Lines { get; set; } = 3;
}

/// <summary>
/// プロパティを読み取り専用としてマークします。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ReadOnlyAttribute : Attribute
{
}