using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Model;

namespace NodeGraph.App.Models;

public partial class EditorPort : ObservableObject
{
    public EditorPort(string name, Port port)
    {
        Port = port;
        Name = name;
        IsInput = Port is InputPort or ExecInPort;
        IsExecPort = Port is ExecInPort or ExecOutPort;
        Value = Port.ValueString;
    }

    public bool IsInput { get; }
    public bool IsOutput => !IsInput;
    public bool IsExecPort { get; }

    public PortId Id => Port.Id;
    public Port Port { get; }

    public string Name { get; }

    [ObservableProperty] public partial string Value { get; set; } = string.Empty;

    public string TypeName
    {
        get
        {
            // ExecPortの場合は"Exec"を返す
            if (Port is ExecInPort or ExecOutPort) return "Exec";

            var type = Port.GetType();
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
        Value = Port.ValueString;
    }
}