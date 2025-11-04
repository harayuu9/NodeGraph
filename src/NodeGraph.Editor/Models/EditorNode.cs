using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Editor.Selection;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorNode : ObservableObject, ISelectable, IRectangular
{
    private readonly Guid _id = Guid.NewGuid();

    public string Name => _node.GetType().Name;

    /// <summary>
    /// この EditorNode の一意な識別子
    /// </summary>
    public object SelectionId => _id;

    public SelectionManager SelectionManager { get; }

    public Node Node => _node;

    public ObservableCollection<EditorPort> InputPorts { get; }
    public ObservableCollection<EditorPort> OutputPorts { get; }

    [ObservableProperty] public partial double X { get; set; }
    [ObservableProperty] public partial double Y { get; set; }
    [ObservableProperty] public partial double Width { get; set; }
    [ObservableProperty] public partial double Height { get; set; }

    private readonly Node _node;

    public EditorNode(SelectionManager selectionManager, Node node)
    {
        _node = node;
        SelectionManager = selectionManager;
        InputPorts = new ObservableCollection<EditorPort>(node.InputPorts.Select((x, i) => new EditorPort(node.GetInputPortName(i), x)));
        OutputPorts = new ObservableCollection<EditorPort>(node.OutputPorts.Select((x, i) => new EditorPort(node.GetOutputPortName(i), x)));
    }

    public void UpdatePortValues()
    {
        foreach (var port in InputPorts)
        {
            port.UpdateValue();
        }
        foreach (var port in OutputPorts)
        {
            port.UpdateValue();
        }
    }
}