using NodeGraph.Model;

namespace NodeGraph.UnitTest;

[Node]
public partial class ConstantNode
{
    [Output] private int _value;

    public void SetValue(int value)
    {
        _value = value;
    }
    
    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

[Node]
public partial class ResultNode
{
    [Input] private int _value;
    public int Value => _value;
    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

[Node]
public partial class AddNode
{
    [Input] private int _a = 50;
    [Input] private int _b = 100;
    
    [Output] private int _result;
    public int Result => _result;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        _result = _a + _b;
        return Task.CompletedTask;
    }
}

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var graph = new Graph();

        var a = graph.CreateNode<ConstantNode>();
        a.SetValue(100);
        var b = graph.CreateNode<ConstantNode>();
        b.SetValue(200);
        var c = graph.CreateNode<ConstantNode>();
        c.SetValue(50);

        var add1 = graph.CreateNode<AddNode>();
        add1.ConnectInput(0, a, 0);
        add1.ConnectInput(1, b, 0);
        
        var add2 = graph.CreateNode<AddNode>();
        add2.ConnectInput(0, add1, 0);
        add2.ConnectInput(1, c, 0);
        
        var result = graph.CreateNode<ResultNode>();
        result.ConnectInput(0, add2, 0);
        
        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var resultNode = graph.GetNodes<ResultNode>().First();
        Assert.Equal(350, resultNode.Value);
        
        a.SetValue(resultNode.Value);
        await executor.ExecuteAsync();
        Assert.Equal(600, resultNode.Value);
    }
}