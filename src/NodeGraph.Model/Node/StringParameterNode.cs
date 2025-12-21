namespace NodeGraph.Model;

/// <summary>
/// 外部から渡された文字列パラメータを取得するノード。
/// </summary>
[Node("String Parameter", "Parameters", HasExecIn = false, HasExecOut = false)]
public partial class StringParameterNode
{
    [Property(DisplayName = "Parameter Name", Tooltip = "外部パラメータ名")]
    private string _parameterName = string.Empty;

    [Output]
    private string _value = string.Empty;

    public void SetParameterName(string name)
    {
        _parameterName = name;
    }

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        if (!string.IsNullOrEmpty(_parameterName))
            _value = context.GetParameter<string>(_parameterName) ?? string.Empty;
        return Task.CompletedTask;
    }
}
