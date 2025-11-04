using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Model;

namespace NodeGraph.Editor.Models;

public partial class EditorPort : ObservableObject
{
    private readonly Port _port;

    public EditorPort(string name, Port port)
    {
        _port = port;
        Name = name;
        IsInput = _port is InputPort;
        Value = _port.ValueString;
    }

    public bool IsInput { get; }
    public bool IsOutput => !IsInput;

    public PortId Id => _port.Id;
    public Port Port => _port;

    public string Name { get; }

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    public string TypeName
    {
        get
        {
            var type = _port.GetType();
            if (type.IsGenericType)
            {
                var genericArg = type.GetGenericArguments()[0];
                return genericArg.Name;
            }
            return "Unknown";
        }
    }

    public void UpdateValue()
    {
        Value = _port.ValueString;
    }
}
