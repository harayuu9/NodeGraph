namespace NodeGraph.Model;

/// <summary>
/// 外部から渡された真偽値パラメータを取得するノード。
/// </summary>
[Node("Bool Parameter", "Parameters", HasExecIn = false, HasExecOut = false)]
public partial class BoolParameterNode
{
    [Property(DisplayName = "Parameter Name", Tooltip = "外部パラメータ名")]
    private string _parameterName = string.Empty;

    [Property(DisplayName = "Default Value", Tooltip = "パラメータ未設定時のデフォルト値")]
    private bool _defaultValue;

    [Output]
    private bool _value;

    public void SetParameterName(string name)
    {
        _parameterName = name;
    }

    public void SetDefaultValue(bool value)
    {
        _defaultValue = value;
    }

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        if (!string.IsNullOrEmpty(_parameterName) &&
            context.TryGetParameter<bool>(_parameterName, out var paramValue))
        {
            _value = paramValue;
        }
        else
        {
            _value = _defaultValue;
        }
        return Task.CompletedTask;
    }
}
