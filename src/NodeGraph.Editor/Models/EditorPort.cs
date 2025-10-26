using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorPort : ObservableObject
{
    private readonly InputPort? _inputPort;
    private readonly OutputPort? _outputPort;

    private EditorPort(InputPort inputPort)
    {
        _inputPort = inputPort;
        _outputPort = null;
        IsInput = true;
    }

    private EditorPort(OutputPort outputPort)
    {
        _inputPort = null;
        _outputPort = outputPort;
        IsInput = false;
    }

    public static EditorPort FromInput(InputPort port) => new(port);
    public static EditorPort FromOutput(OutputPort port) => new(port);

    public bool IsInput { get; }
    public bool IsOutput => !IsInput;

    public PortId Id => IsInput ? _inputPort!.Id : _outputPort!.Id;

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

    public InputPort? InputPort => _inputPort;
    public OutputPort? OutputPort => _outputPort;
}
