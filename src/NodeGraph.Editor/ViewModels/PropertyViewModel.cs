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
    private readonly PropertyDescriptor _descriptor;

    [ObservableProperty]
    private object? _value;

    public PropertyViewModel(Node node, PropertyDescriptor descriptor)
    {
        _node = node;
        _descriptor = descriptor;
        _value = descriptor.Getter(node);
    }

    /// <summary>
    /// プロパティの表示名を取得します。
    /// </summary>
    public string DisplayName => _descriptor.DisplayName;

    /// <summary>
    /// プロパティの型を取得します。
    /// </summary>
    public Type PropertyType => _descriptor.Type;

    /// <summary>
    /// プロパティ記述子を取得します。
    /// </summary>
    public PropertyDescriptor Descriptor => _descriptor;

    /// <summary>
    /// ツールチップテキストを取得します。
    /// </summary>
    public string? Tooltip => _descriptor.Tooltip;

    /// <summary>
    /// 読み取り専用かどうかを取得します。
    /// </summary>
    public bool IsReadOnly => _descriptor.HasAttribute<ReadOnlyAttribute>();

    partial void OnValueChanged(object? value)
    {
        if (value == null)
        {
            _descriptor.Setter(_node, null);
            return;
        }

        // 型変換を行う
        try
        {
            var convertedValue = Convert.ChangeType(value, _descriptor.Type);
            _descriptor.Setter(_node, convertedValue);
        }
        catch (Exception)
        {
            // 変換失敗時はそのまま渡す（元のSetterでエラーが出る可能性がある）
            _descriptor.Setter(_node, value);
        }
    }

    /// <summary>
    /// ノードから最新の値を読み込みます。
    /// </summary>
    public void RefreshValue()
    {
        Value = _descriptor.Getter(_node);
    }
}
