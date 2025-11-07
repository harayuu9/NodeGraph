namespace NodeGraph.Model;

[Node]
public partial class StringConstantNode
{
    [Property(DisplayName = "Value", Tooltip = "定数値")]
    [Output]
    private string _value;
    
    public void SetValue(string value)
    {
        _value = value;
    }
}