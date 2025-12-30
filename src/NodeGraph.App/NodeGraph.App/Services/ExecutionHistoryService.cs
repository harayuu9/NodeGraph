using System.Collections.ObjectModel;
using NodeGraph.App.Serialization;

namespace NodeGraph.App.Services;

public class ExecutionHistoryService
{
    private string _historyFilePath;
    private readonly ObservableCollection<ExecutionHistory> _histories = [];

    public ObservableCollection<ExecutionHistory> Histories => _histories;

    public ExecutionHistoryService(ConfigService config)
    {
        _historyFilePath = config.HistoryDirectory;
    }

    public void Add(ExecutionHistory history)
    {
        _histories.Add(history);
    }
}