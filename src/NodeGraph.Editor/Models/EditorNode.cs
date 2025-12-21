using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.ViewModels;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorNode : ObservableObject, ISelectable, IPositionable
{
    public EditorNode(SelectionManager selectionManager, Node node)
    {
        Node = node;
        SelectionManager = selectionManager;
        InputPorts = new ObservableCollection<EditorPort>(node.InputPorts.Select((x, i) => new EditorPort(node.GetInputPortName(i), x)));
        OutputPorts = new ObservableCollection<EditorPort>(node.OutputPorts.Select((x, i) => new EditorPort(node.GetOutputPortName(i), x)));

        // 全ノードがExecポートを持つ
        ExecInPorts = new ObservableCollection<EditorPort>(node.ExecInPorts.Select((x, i) => new EditorPort(node.GetExecInPortName(i), x)));
        ExecOutPorts = new ObservableCollection<EditorPort>(node.ExecOutPorts.Select((x, i) => new EditorPort(node.GetExecOutPortName(i), x)));

        Properties = new ObservableCollection<PropertyViewModel>(
            node.GetProperties().Select(descriptor => new PropertyViewModel(node, descriptor))
        );
    }

    public string Name => Node.GetDisplayName();

    public SelectionManager SelectionManager { get; }

    public Node Node { get; }

    public ObservableCollection<EditorPort> InputPorts { get; }
    public ObservableCollection<EditorPort> OutputPorts { get; }
    public ObservableCollection<EditorPort> ExecInPorts { get; }
    public ObservableCollection<EditorPort> ExecOutPorts { get; }
    public ObservableCollection<PropertyViewModel> Properties { get; }
    [ObservableProperty] public partial ExecutionStatus ExecutionStatus { get; set; }

    [ObservableProperty] public partial double X { get; set; }
    [ObservableProperty] public partial double Y { get; set; }

    /// <summary>
    /// この EditorNode の一意な識別子
    /// </summary>
    public object SelectionId => Node.Id;

    public void UpdatePortValues()
    {
        foreach (var port in InputPorts) port.UpdateValue();
        foreach (var port in OutputPorts) port.UpdateValue();
    }

    public EditorNode Clone()
    {
        var nodeType = Node.GetType();

        if (Activator.CreateInstance(nodeType) is not Node newNode) throw new InvalidOperationException("Failed to create a new instance of the node type.");

        var properties = Node.GetProperties();
        foreach (var property in properties)
            try
            {
                var value = property.GetValue(Node);
                property.SetValue(newNode, value);
            }
            catch
            {
                // ignored
            }

        var newEditorNode = new EditorNode(SelectionManager, newNode)
        {
            X = X + 30,
            Y = Y + 30
        };
        return newEditorNode;
    }
}