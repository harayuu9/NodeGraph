using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorNode(Node node) : ObservableObject
{
    public string Name => node.GetType().Name;
    
    [ObservableProperty] private double _positionX;
    [ObservableProperty] private double _positionY;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
}