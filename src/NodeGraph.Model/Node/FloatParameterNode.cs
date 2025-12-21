namespace NodeGraph.Model;

/// <summary>
/// 外部から渡された浮動小数点パラメータを取得するノード。
/// </summary>
[Node("Float Parameter", "Parameters", HasExecIn = false, HasExecOut = false)]
public partial class FloatParameterNode
{
    [Property(DisplayName = "Parameter Name", Tooltip = "外部パラメータ名")]
    private string _parameterName = string.Empty;

    [Property(DisplayName = "Default Value", Tooltip = "パラメータ未設定時のデフォルト値")]
    private float _defaultValue;

    [Output]
    private float _value;

    public void SetParameterName(string name)
    {
        _parameterName = name;
    }

    public void SetDefaultValue(float value)
    {
        _defaultValue = value;
    }

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        if (!string.IsNullOrEmpty(_parameterName) &&
            context.TryGetParameter<float>(_parameterName, out var paramValue))
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
