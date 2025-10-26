using System;
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
    
    [ObservableProperty] private double _positionX;
    [ObservableProperty] private double _positionY;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
}