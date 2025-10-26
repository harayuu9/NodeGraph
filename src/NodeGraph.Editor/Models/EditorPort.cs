using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorPort : ObservableObject
{
    private readonly InputPort? _inputPort;
    private readonly OutputPort? _outputPort;

    private EditorPort(string name, InputPort inputPort)
    {
        _inputPort = inputPort;
        _outputPort = null;
        IsInput = true;
        Name = name;
    }

    private EditorPort(string name, OutputPort outputPort)
    {
        _inputPort = null;
        _outputPort = outputPort;
        IsInput = false;
        Name = name;
    }

    public static EditorPort FromInput(string name, InputPort port) => new(name, port);
    public static EditorPort FromOutput(string name, OutputPort port) => new(name, port);

    public bool IsInput { get; }
    public bool IsOutput => !IsInput;

    public PortId Id => IsInput ? _inputPort!.Id : _outputPort!.Id;

    public string Name { get; }
    public string TypeName
    {
        get
        {
            var port = (object?)_inputPort ?? _outputPort;
            if (port == null) return "Unknown";

            var type = port.GetType();
            if (type.IsGenericType)
            {
                var genericArg = type.GetGenericArguments()[0];
                return genericArg.Name;
            }
            return "Unknown";
        }
    }
}
