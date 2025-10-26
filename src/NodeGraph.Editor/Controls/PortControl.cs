using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using NodeGraph.Editor.Models;
using NodeGraph.Model;

namespace NodeGraph.Editor.Controls;

public enum PortDirection
{
    Left,
    Right
}

public class PortControl : TemplatedControl
{
    public static readonly StyledProperty<EditorPort?> PortProperty = AvaloniaProperty.Register<PortControl, EditorPort?>(nameof(Port));
    public static readonly StyledProperty<PortDirection> DirectionProperty = AvaloniaProperty.Register<PortControl, PortDirection>(nameof(Direction));

    public PortControl()
    {
        if (Design.IsDesignMode)
        {
            var n = new FloatAddNode();
            n.Initialize();
            Port = EditorPort.FromInput(n.InputPorts[0]);
        }
    }
    
    public EditorPort? Port
    {
        get => GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }
    
    public PortDirection Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }
}
