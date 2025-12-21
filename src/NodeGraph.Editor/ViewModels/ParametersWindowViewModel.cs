using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.Editor.Services;

namespace NodeGraph.Editor.ViewModels;

/// <summary>
/// パラメータ設定ウィンドウのViewModel
/// </summary>
public partial class ParametersWindowViewModel : ObservableObject
{
    private readonly CommonParameterService _commonParameterService;

    public ParametersWindowViewModel(CommonParameterService commonParameterService)
    {
        _commonParameterService = commonParameterService;
        LoadParameters();
    }

    public ObservableCollection<ParameterItemViewModel> Parameters { get; } = [];

    [ObservableProperty]
    public partial ParameterItemViewModel? SelectedParameter { get; set; }

    [ObservableProperty]
    public partial string NewParameterName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewParameterValue { get; set; } = string.Empty;

    /// <summary>
    /// パラメータをサービスから読み込む
    /// </summary>
    private void LoadParameters()
    {
        // 既存のイベントを解除
        foreach (var param in Parameters)
        {
            param.ValueChanged -= OnParameterValueChanged;
        }

        Parameters.Clear();

        // 環境変数からのパラメータ
        var allParams = _commonParameterService.GetParameters();
        var fileParams = _commonParameterService.GetFileParameters();

        foreach (var kvp in allParams)
        {
            var isFromEnv = !fileParams.ContainsKey(kvp.Key);
            var item = new ParameterItemViewModel
            {
                Name = kvp.Key,
                Value = kvp.Value?.ToString() ?? string.Empty,
                IsFromEnvironment = isFromEnv
            };
            item.ValueChanged += OnParameterValueChanged;
            Parameters.Add(item);
        }
    }

    /// <summary>
    /// パラメータの値が変更されたときの処理
    /// </summary>
    private void OnParameterValueChanged(ParameterItemViewModel item)
    {
        _commonParameterService.SetParameter(item.Name, item.Value);
    }

    /// <summary>
    /// 新しいパラメータを追加
    /// </summary>
    [RelayCommand]
    private void AddParameter()
    {
        if (string.IsNullOrWhiteSpace(NewParameterName))
            return;

        var name = NewParameterName.Trim();

        // 既存のパラメータを更新または新規追加
        _commonParameterService.SetParameter(name, NewParameterValue);

        // リスト更新
        var existing = Parameters.FirstOrDefault(p => p.Name == name);
        if (existing != null)
        {
            existing.Value = NewParameterValue;
            existing.IsFromEnvironment = false;
        }
        else
        {
            var item = new ParameterItemViewModel
            {
                Name = name,
                Value = NewParameterValue,
                IsFromEnvironment = false
            };
            item.ValueChanged += OnParameterValueChanged;
            Parameters.Add(item);
        }

        // 入力フィールドをクリア
        NewParameterName = string.Empty;
        NewParameterValue = string.Empty;
    }

    /// <summary>
    /// 選択したパラメータを削除
    /// </summary>
    [RelayCommand]
    private void DeleteParameter()
    {
        if (SelectedParameter == null)
            return;

        if (SelectedParameter.IsFromEnvironment)
            return; // 環境変数は削除不可

        SelectedParameter.ValueChanged -= OnParameterValueChanged;
        _commonParameterService.RemoveParameter(SelectedParameter.Name);
        Parameters.Remove(SelectedParameter);
        SelectedParameter = null;
    }

    /// <summary>
    /// パラメータの値を保存（編集後）
    /// </summary>
    [RelayCommand]
    private void SaveParameter(ParameterItemViewModel parameter)
    {
        if (parameter.IsFromEnvironment)
            return; // 環境変数は編集不可

        _commonParameterService.SetParameter(parameter.Name, parameter.Value);
    }

    /// <summary>
    /// 全てのパラメータを再読み込み
    /// </summary>
    [RelayCommand]
    private void Reload()
    {
        _commonParameterService.Reload();
        LoadParameters();
    }
}
