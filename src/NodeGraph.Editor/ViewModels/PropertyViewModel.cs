using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Model;

namespace NodeGraph.Editor.ViewModels;

/// <summary>
/// ノードプロパティのViewModelを表します。
/// </summary>
public partial class PropertyViewModel : ObservableObject
{
    private readonly Node _node;

    public PropertyViewModel(Node node, PropertyDescriptor descriptor)
    {
        _node = node;
        Descriptor = descriptor;
        Value = descriptor.Getter(node);
    }

    [ObservableProperty] public partial object? Value { get; set; }

    /// <summary>
    /// プロパティの表示名を取得します。
    /// </summary>
    public string DisplayName => Descriptor.DisplayName;

    /// <summary>
    /// プロパティの型を取得します。
    /// </summary>
    public Type PropertyType => Descriptor.Type;

    /// <summary>
    /// プロパティ記述子を取得します。
    /// </summary>
    public PropertyDescriptor Descriptor { get; }

    /// <summary>
    /// ツールチップテキストを取得します。
    /// </summary>
    public string? Tooltip => Descriptor.Tooltip;

    /// <summary>
    /// 読み取り専用かどうかを取得します。
    /// </summary>
    public bool IsReadOnly => Descriptor.HasAttribute<ReadOnlyAttribute>();

    /// <summary>
    /// ノード内に表示するかどうかを取得します。
    /// </summary>
    public bool ShowInNode => Descriptor.ShowInNode;

    /// <summary>
    /// Inspectorに表示するかどうかを取得します。
    /// </summary>
    public bool ShowInInspector => Descriptor.ShowInInspector;

    partial void OnValueChanged(object? value)
    {
        if (value == null)
        {
            Descriptor.Setter(_node, null);
            return;
        }

        // 型変換を行う
        try
        {
            var convertedValue = Convert.ChangeType(value, Descriptor.Type);
            Descriptor.Setter(_node, convertedValue);
        }
        catch (Exception)
        {
            // 変換失敗時はそのまま渡す（元のSetterでエラーが出る可能性がある）
            Descriptor.Setter(_node, value);
        }
    }

    /// <summary>
    /// ノードから最新の値を読み込みます。
    /// </summary>
    public void RefreshValue()
    {
        Value = Descriptor.Getter(_node);
    }
}