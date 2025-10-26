using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Editor.Selection;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorNode(SelectionManager selectionManager, Node node) : ObservableObject, ISelectable
{
    private readonly Guid _id = Guid.NewGuid();

    public string Name => node.GetType().Name;

    /// <summary>
    /// この EditorNode の一意な識別子
    /// </summary>
    public object SelectionId => _id;

    public SelectionManager SelectionManager { get; } = selectionManager;

    public Node Node => node;

    public ObservableCollection<EditorPort> InputPorts { get; } = new(node.InputPorts.Select(EditorPort.FromInput));
    public ObservableCollection<EditorPort> OutputPorts { get; } = new(node.OutputPorts.Select(EditorPort.FromOutput));

    [ObservableProperty] private double _positionX;
    [ObservableProperty] private double _positionY;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
}