using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NodeGraph.Editor.ViewModels;

/// <summary>
/// パラメータ項目のViewModel
/// </summary>
public partial class ParameterItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsFromEnvironment { get; set; }

    /// <summary>
    /// 値が変更されたときに発火するイベント
    /// </summary>
    public event Action<ParameterItemViewModel>? ValueChanged;

    partial void OnValueChanged(string value)
    {
        if (!IsFromEnvironment)
        {
            ValueChanged?.Invoke(this);
        }
    }
}
