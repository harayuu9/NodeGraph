using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.App.Services;

namespace NodeGraph.App.ViewModels;

/// <summary>
/// グラフ一覧の各項目を表すViewModel
/// </summary>
public partial class GraphListItemViewModel : ObservableObject
{
    [ObservableProperty] private string _id;
    [ObservableProperty] private string _name;
    [ObservableProperty] private DateTime _modifiedAt;
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private string _editingName = "";

    public GraphListItemViewModel(string id, string name, DateTime modifiedAt)
    {
        _id = id;
        _name = name;
        _modifiedAt = modifiedAt;
    }

    public GraphListItemViewModel(GraphMetadata metadata)
        : this(metadata.Id, metadata.Name, metadata.ModifiedAt)
    {
    }

    /// <summary>
    /// 名前変更モードを開始
    /// </summary>
    [RelayCommand]
    private void StartRename()
    {
        EditingName = Name;
        IsRenaming = true;
    }

    /// <summary>
    /// 名前変更を確定
    /// </summary>
    [RelayCommand]
    private void ConfirmRename()
    {
        if (!string.IsNullOrWhiteSpace(EditingName))
        {
            Name = EditingName.Trim();
        }
        IsRenaming = false;
        RenameConfirmed?.Invoke(this);
    }

    /// <summary>
    /// 名前変更をキャンセル
    /// </summary>
    [RelayCommand]
    private void CancelRename()
    {
        IsRenaming = false;
        EditingName = Name;
    }

    /// <summary>
    /// 名前変更が確定されたときに発生するイベント
    /// </summary>
    public event Action<GraphListItemViewModel>? RenameConfirmed;
}
