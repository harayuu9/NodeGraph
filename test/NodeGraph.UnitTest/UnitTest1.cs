using NodeGraph.Model;

namespace NodeGraph.UnitTest;

[Node]
public partial class AddNode
{
    [Input] private int _a = 50;
    [Input] private int _b = 100;
    
    [Output] private int _result;
    public int Result => _result;

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
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
        var node1 = new AddNode();
        node1.Initialize();
        var node2 = new AddNode();
        node2.Initialize();
        
        node2.ConnectInput(0, node1, 0);
        node2.ConnectInput(1, node1, 0);
        
        await node1.ExecuteNodeAsync(CancellationToken.None);
        await node2.ExecuteNodeAsync(CancellationToken.None);
        Assert.Equal(150, node1.Result);
        Assert.Equal(300, node2.Result);
    }
}