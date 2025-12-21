namespace NodeGraph.Model;

/// <summary>
/// ノードプロパティのメタデータと値アクセサを提供します。
/// </summary>
public sealed class PropertyDescriptor
{
    /// <summary>
    /// プロパティ名を取得します。
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// プロパティの型を取得します。
    /// </summary>
    public Type Type { get; set; } = typeof(object);

    /// <summary>
    /// プロパティ値を取得するデリゲートを取得します。
    /// </summary>
    public Func<Node, object?> Getter { get; set; } = _ => null;

    /// <summary>
    /// プロパティ値を設定するデリゲートを取得します。
    /// </summary>
    public Action<Node, object?> Setter { get; set; } = (_, _) => { };

    /// <summary>
    /// プロパティに適用されている属性の配列を取得します。
    /// </summary>
    public Attribute[] Attributes { get; set; } = [];

    /// <summary>
    /// 表示名を取得します。指定されていない場合はNameを返します。
    /// </summary>
    public string DisplayName
    {
        get
        {
            var propertyAttr = GetAttribute<PropertyAttribute>();
            return propertyAttr?.DisplayName ?? Name;
        }
    }

    /// <summary>
    /// カテゴリ名を取得します。
    /// </summary>
    public string? Category
    {
        get
        {
            var propertyAttr = GetAttribute<PropertyAttribute>();
            return propertyAttr?.Category;
        }
    }

    /// <summary>
    /// ツールチップテキストを取得します。
    /// </summary>
    public string? Tooltip
    {
        get
        {
            var propertyAttr = GetAttribute<PropertyAttribute>();
            return propertyAttr?.Tooltip;
        }
    }

    /// <summary>
    /// ノード内に表示するかどうかを取得します。
    /// </summary>
    public bool ShowInNode
    {
        get
        {
            var propertyAttr = GetAttribute<PropertyAttribute>();
            return propertyAttr?.ShowInNode ?? true;
        }
    }

    /// <summary>
    /// Inspectorに表示するかどうかを取得します。
    /// </summary>
    public bool ShowInInspector
    {
        get
        {
            var propertyAttr = GetAttribute<PropertyAttribute>();
            return propertyAttr?.ShowInInspector ?? false;
        }
    }

    /// <summary>
    /// 指定した型の属性を取得します。
    /// </summary>
    /// <typeparam name="T">属性の型</typeparam>
    /// <returns>属性インスタンス、存在しない場合はnull</returns>
    public T? GetAttribute<T>() where T : Attribute
    {
        return Attributes.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// 指定した型の属性が存在するかを確認します。
    /// </summary>
    /// <typeparam name="T">属性の型</typeparam>
    /// <returns>属性が存在する場合true</returns>
    public bool HasAttribute<T>() where T : Attribute
    {
        return Attributes.OfType<T>().Any();
    }

    /// <summary>
    /// プロパティの値を取得します。
    /// </summary>
    /// <param name="node">対象のノード</param>
    /// <returns>プロパティ値</returns>
    public object? GetValue(Node node)
    {
        return Getter(node);
    }

    /// <summary>
    /// プロパティの値を設定します。
    /// </summary>
    /// <param name="node">対象のノード</param>
    /// <param name="value">設定する値</param>
    public void SetValue(Node node, object? value)
    {
        Setter(node, value);
    }
}