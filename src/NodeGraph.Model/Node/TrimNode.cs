namespace NodeGraph.Model;

public enum TrimMode
{
    Both,
    Start,
    End
}

[Node("Trim", "String")]
public partial class TrimNode
{
    [Input] private string _input = string.Empty;

    [Property(DisplayName = "Mode", Tooltip = "Trim対象: Both/Start/End")]
    private TrimMode _mode = TrimMode.Both;

    [Output] private string _result = string.Empty;

    public void SetMode(TrimMode mode)
    {
        _mode = mode;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;

        _result = _mode switch
        {
            TrimMode.Start => input.TrimStart(),
            TrimMode.End => input.TrimEnd(),
            _ => input.Trim()
        };

        await context.ExecuteOutAsync(0);
    }
}
