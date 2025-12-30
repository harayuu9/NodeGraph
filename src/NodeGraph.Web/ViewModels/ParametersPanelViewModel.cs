using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.Web.Services;

namespace NodeGraph.Web.ViewModels;

/// <summary>
/// パラメータパネルのViewModel
/// </summary>
public partial class ParametersPanelViewModel : ViewModelBase
{
    private readonly BrowserParameterService _parameterService;

    public ObservableCollection<ParameterItemViewModel> Parameters { get; } = [];
    public ObservableCollection<string> Presets { get; } = [];

    [ObservableProperty]
    private ParameterItemViewModel? _selectedParameter;

    [ObservableProperty]
    private string? _selectedPreset;

    [ObservableProperty]
    private string _newParameterName = string.Empty;

    [ObservableProperty]
    private string _newParameterValue = string.Empty;

    [ObservableProperty]
    private string _newPresetName = string.Empty;

    public ParametersPanelViewModel(BrowserParameterService parameterService)
    {
        _parameterService = parameterService;
        LoadParameters();
        LoadPresets();
    }

    /// <summary>
    /// パラメータを読み込む
    /// </summary>
    private void LoadParameters()
    {
        foreach (var param in Parameters)
        {
            param.ValueChanged -= OnParameterValueChanged;
        }

        Parameters.Clear();

        var allParams = _parameterService.GetParameters();
        foreach (var kvp in allParams)
        {
            var item = new ParameterItemViewModel
            {
                Name = kvp.Key,
                Value = kvp.Value?.ToString() ?? string.Empty
            };
            item.ValueChanged += OnParameterValueChanged;
            Parameters.Add(item);
        }
    }

    private async void OnParameterValueChanged(ParameterItemViewModel item)
    {
        await _parameterService.SetParameterAsync(item.Name, item.Value);
    }

    [RelayCommand]
    private async Task AddParameterAsync()
    {
        if (string.IsNullOrWhiteSpace(NewParameterName))
            return;

        var name = NewParameterName.Trim();
        await _parameterService.SetParameterAsync(name, NewParameterValue);

        var existing = Parameters.FirstOrDefault(p => p.Name == name);
        if (existing != null)
        {
            existing.Value = NewParameterValue;
        }
        else
        {
            var item = new ParameterItemViewModel
            {
                Name = name,
                Value = NewParameterValue
            };
            item.ValueChanged += OnParameterValueChanged;
            Parameters.Add(item);
        }

        NewParameterName = string.Empty;
        NewParameterValue = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteParameterAsync()
    {
        if (SelectedParameter == null)
            return;

        SelectedParameter.ValueChanged -= OnParameterValueChanged;
        await _parameterService.RemoveParameterAsync(SelectedParameter.Name);
        Parameters.Remove(SelectedParameter);
        SelectedParameter = null;
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        await _parameterService.ReloadAsync();
        LoadParameters();
        await _parameterService.ReloadPresetsAsync();
        LoadPresets();
    }

    // ===== プリセット機能 =====

    /// <summary>
    /// プリセット一覧を読み込む
    /// </summary>
    private void LoadPresets()
    {
        Presets.Clear();
        foreach (var name in _parameterService.GetPresetNames())
        {
            Presets.Add(name);
        }
    }

    [RelayCommand]
    private async Task SavePresetAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPresetName))
            return;

        var name = NewPresetName.Trim();
        await _parameterService.SavePresetAsync(name);

        if (!Presets.Contains(name))
        {
            Presets.Add(name);
        }

        NewPresetName = string.Empty;
    }

    [RelayCommand]
    private async Task LoadPresetAsync()
    {
        if (string.IsNullOrEmpty(SelectedPreset))
            return;

        await _parameterService.LoadPresetAsync(SelectedPreset);
        LoadParameters();
    }

    [RelayCommand]
    private async Task DeletePresetAsync()
    {
        if (string.IsNullOrEmpty(SelectedPreset))
            return;

        await _parameterService.DeletePresetAsync(SelectedPreset);
        Presets.Remove(SelectedPreset);
        SelectedPreset = null;
    }
}

/// <summary>
/// パラメータ項目のViewModel
/// </summary>
public partial class ParameterItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    public event Action<ParameterItemViewModel>? ValueChanged;

    partial void OnValueChanged(string value)
    {
        ValueChanged?.Invoke(this);
    }
}
