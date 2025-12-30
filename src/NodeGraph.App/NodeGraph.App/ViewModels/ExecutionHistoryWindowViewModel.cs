using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.App.Models;
using NodeGraph.App.Selection;
using NodeGraph.App.Serialization;
using NodeGraph.App.Services;

namespace NodeGraph.App.ViewModels;

public partial class ExecutionHistoryWindowViewModel : ViewModelBase
{
    public ExecutionHistoryWindowViewModel(ExecutionHistoryService historyService)
    {
        Histories = historyService.Histories;
    }

    public ObservableCollection<ExecutionHistory> Histories { get; }

    [ObservableProperty]
    public partial ExecutionHistory? SelectedHistory { get; set; }

    [ObservableProperty]
    public partial EditorGraph? CurrentGraph { get; set; }

    [ObservableProperty]
    public partial int CurrentStepIndex { get; set; }

    [ObservableProperty]
    public partial EditorNode? CurrentStepNode { get; set; }

    public ObservableCollection<PortValueItem> CurrentInputValues { get; } = [];
    public ObservableCollection<PortValueItem> CurrentOutputValues { get; } = [];

    public int TotalSteps => SelectedHistory?.Histories.Count ?? 0;

    /// <summary>
    /// 1-based のステップ番号（UI表示用）
    /// </summary>
    public int CurrentStepNumber => CurrentStepIndex + 1;

    public bool HasHistory => SelectedHistory != null && TotalSteps > 0;

    partial void OnSelectedHistoryChanged(ExecutionHistory? value)
    {
        if (value == null)
        {
            CurrentGraph = null;
            CurrentStepIndex = 0;
            CurrentStepNode = null;
            CurrentInputValues.Clear();
            CurrentOutputValues.Clear();
            OnPropertyChanged(nameof(TotalSteps));
            OnPropertyChanged(nameof(HasHistory));
            return;
        }

        var selectionManager = new SelectionManager();
        CurrentGraph = value.Create(selectionManager);
        OnPropertyChanged(nameof(TotalSteps));
        OnPropertyChanged(nameof(HasHistory));

        // Reset to first step (this will trigger visualization update)
        CurrentStepIndex = 0;
        UpdateVisualization();
    }

    partial void OnCurrentStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentStepNumber));
        UpdateVisualization();
    }

    private void UpdateVisualization()
    {
        if (SelectedHistory == null || CurrentGraph == null || TotalSteps == 0)
        {
            CurrentStepNode = null;
            CurrentInputValues.Clear();
            CurrentOutputValues.Clear();
            return;
        }

        // 1. Reset all nodes to Waiting state
        foreach (var node in CurrentGraph.Nodes)
            node.ExecutionStatus = ExecutionStatus.Waiting;

        // 2. Mark nodes up to current step as Executed
        for (var i = 0; i < CurrentStepIndex; i++)
        {
            var history = SelectedHistory.Histories[i];
            var editorNode = CurrentGraph.Nodes.FirstOrDefault(n => n.Node.Id == history.NodeId);
            if (editorNode != null)
                editorNode.ExecutionStatus = ExecutionStatus.Executed;
        }

        // 3. Mark current step node as Executing (highlighted)
        if (CurrentStepIndex < TotalSteps)
        {
            var currentHistory = SelectedHistory.Histories[CurrentStepIndex];
            CurrentStepNode = CurrentGraph.Nodes.FirstOrDefault(n => n.Node.Id == currentHistory.NodeId);

            if (CurrentStepNode != null)
            {
                CurrentStepNode.ExecutionStatus = ExecutionStatus.Executing;
                UpdatePortValues(currentHistory);
            }
        }
        else
        {
            CurrentStepNode = null;
            CurrentInputValues.Clear();
            CurrentOutputValues.Clear();
        }
    }

    private void UpdatePortValues(ExecutionHistory.History history)
    {
        CurrentInputValues.Clear();
        CurrentOutputValues.Clear();

        if (CurrentStepNode == null) return;

        // Map input values
        for (var i = 0; i < history.InputValues.Length && i < CurrentStepNode.InputPorts.Count; i++)
        {
            CurrentInputValues.Add(new PortValueItem(
                CurrentStepNode.InputPorts[i].Name,
                history.InputValues[i]?.ToString() ?? "null",
                CurrentStepNode.InputPorts[i].TypeName
            ));
        }

        // Map output values
        for (var i = 0; i < history.OutputValues.Length && i < CurrentStepNode.OutputPorts.Count; i++)
        {
            CurrentOutputValues.Add(new PortValueItem(
                CurrentStepNode.OutputPorts[i].Name,
                history.OutputValues[i]?.ToString() ?? "null",
                CurrentStepNode.OutputPorts[i].TypeName
            ));
        }
    }

    [RelayCommand]
    private void GoToFirst()
    {
        if (TotalSteps > 0)
            CurrentStepIndex = 0;
    }

    [RelayCommand]
    private void GoToPrevious()
    {
        if (CurrentStepIndex > 0)
            CurrentStepIndex--;
    }

    [RelayCommand]
    private void GoToNext()
    {
        if (CurrentStepIndex < TotalSteps - 1)
            CurrentStepIndex++;
    }

    [RelayCommand]
    private void GoToLast()
    {
        if (TotalSteps > 0)
            CurrentStepIndex = TotalSteps - 1;
    }
}
