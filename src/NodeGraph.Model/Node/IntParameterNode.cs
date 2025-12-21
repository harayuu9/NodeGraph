namespace NodeGraph.Model;

/// <summary>
/// 外部から渡された整数パラメータを取得するノード。
/// </summary>
[Node("Int Parameter", "Parameters", HasExecIn = false, HasExecOut = false)]
public partial class IntParameterNode
{
    [Property(DisplayName = "Parameter Name", Tooltip = "外部パラメータ名")]
    private string _parameterName = string.Empty;

    [Property(DisplayName = "Default Value", Tooltip = "パラメータ未設定時のデフォルト値")]
    private int _defaultValue;

    [Output]
    private int _value;

    public void SetParameterName(string name)
    {
        _parameterName = name;
    }

    public void SetDefaultValue(int value)
    {
        _defaultValue = value;
    }

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        if (!string.IsNullOrEmpty(_parameterName) &&
            context.TryGetParameter<int>(_parameterName, out var paramValue))
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
