using NodeGraph.Model;

namespace NodeGraph.UnitTest;

public class GraphCloneTest
{
    [Fact]
    public async Task CloneGraph_全ノードをクローン()
    {
        // Arrange
        var graph = new Graph();
        var a = graph.CreateNode<ConstantNode>();
        a.SetValue(100);
        var b = graph.CreateNode<ConstantNode>();
        b.SetValue(200);
        var add = graph.CreateNode<AddNode>();
        add.ConnectInput(0, a, 0);
        add.ConnectInput(1, b, 0);

        // Act
        var clonedGraph = graph.Clone();

        // Assert
        Assert.Equal(3, clonedGraph.Nodes.Count);

        // 元のグラフとクローンが異なるインスタンスであることを確認
        Assert.NotSame(graph.Nodes[0], clonedGraph.Nodes[0]);
        Assert.NotSame(graph.Nodes[1], clonedGraph.Nodes[1]);
        Assert.NotSame(graph.Nodes[2], clonedGraph.Nodes[2]);

        // IDが異なることを確認
        Assert.NotEqual(graph.Nodes[0].Id, clonedGraph.Nodes[0].Id);
        Assert.NotEqual(graph.Nodes[1].Id, clonedGraph.Nodes[1].Id);
        Assert.NotEqual(graph.Nodes[2].Id, clonedGraph.Nodes[2].Id);

        // 実行して結果が同じことを確認
        var executor = clonedGraph.CreateExecutor();
        await executor.ExecuteAsync();

        var clonedAdd = clonedGraph.GetNodes<AddNode>().First();
        Assert.Equal(300, clonedAdd.Result);
    }

    [Fact]
    public async Task CloneGraph_特定のノードのみクローン_接続が保持される()
    {
        // Arrange
        var graph = new Graph();
        var a = graph.CreateNode<ConstantNode>();
        a.SetValue(100);
        var b = graph.CreateNode<ConstantNode>();
        b.SetValue(200);
        var add = graph.CreateNode<AddNode>();
        add.ConnectInput(0, a, 0);
        add.ConnectInput(1, b, 0);

        // Act - a, b, addをクローン（全ノード）
        Node[] nodes = [a, b, add];
        var clonedGraph = Graph.Clone(nodes);

        // Assert
        Assert.Equal(3, clonedGraph.Nodes.Count);

        // 実行して接続が保持されていることを確認
        var executor = clonedGraph.CreateExecutor();
        await executor.ExecuteAsync();

        var clonedAdd = clonedGraph.GetNodes<AddNode>().First();
        Assert.Equal(300, clonedAdd.Result);
    }

    [Fact]
    public async Task CloneGraph_部分的なノードのみクローン_外部接続は除外される()
    {
        // Arrange
        var graph = new Graph();
        var a = graph.CreateNode<ConstantNode>();
        a.SetValue(100);
        var b = graph.CreateNode<ConstantNode>();
        b.SetValue(200);
        var add = graph.CreateNode<AddNode>();
        add.ConnectInput(0, a, 0);
        add.ConnectInput(1, b, 0);

        var result = graph.CreateNode<ResultNode>();
        result.ConnectInput(0, add, 0);

        // Act - a, b, addのみクローン（resultは含めない）
        Node[] nodes = [a, b, add];
        var clonedGraph = Graph.Clone(nodes);

        // Assert
        Assert.Equal(3, clonedGraph.Nodes.Count);

        // addノードの出力が接続されていないことを確認
        var clonedAdd = clonedGraph.GetNodes<AddNode>().First();
        var outputPort = clonedAdd.OutputPorts[0];
        Assert.IsAssignableFrom<MultiConnectPort>(outputPort);
        var multiPort = (MultiConnectPort)outputPort;
        Assert.Empty(multiPort.ConnectedPorts);

        // 実行して内部接続が保持されていることを確認
        var executor = clonedGraph.CreateExecutor();
        await executor.ExecuteAsync();
        Assert.Equal(300, clonedAdd.Result);
    }

    [Fact]
    public async Task CloneGraph_プロパティ値がコピーされる()
    {
        // Arrange
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        constant.SetValue(42.5f);

        // Act
        var clonedGraph = graph.Clone();

        // Assert
        var clonedConstant = clonedGraph.GetNodes<FloatConstantNode>().First();

        // プロパティ値がコピーされていることを確認
        var properties = clonedConstant.GetProperties();
        Assert.NotEmpty(properties);

        var valueProperty = properties[0]; // FloatConstantNodeは_valueプロパティを持つ
        var value = valueProperty.Getter(clonedConstant);
        Assert.Equal(42.5f, value);

        // 実行して値が正しく使われることを確認
        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, constant, 0);

        var clonedResult = clonedGraph.CreateNode<FloatResultNode>();
        clonedResult.ConnectInput(0, clonedConstant, 0);

        await graph.CreateExecutor().ExecuteAsync();
        await clonedGraph.CreateExecutor().ExecuteAsync();

        Assert.Equal(result.Value, clonedResult.Value);
        Assert.Equal(42.5f, clonedResult.Value);
    }

    [Fact]
    public async Task CloneGraph_複雑な接続グラフ()
    {
        // Arrange
        var graph = new Graph();
        var a = graph.CreateNode<ConstantNode>();
        a.SetValue(10);
        var b = graph.CreateNode<ConstantNode>();
        b.SetValue(20);
        var c = graph.CreateNode<ConstantNode>();
        c.SetValue(30);

        var add1 = graph.CreateNode<AddNode>();
        add1.ConnectInput(0, a, 0);
        add1.ConnectInput(1, b, 0);

        var add2 = graph.CreateNode<AddNode>();
        add2.ConnectInput(0, add1, 0);
        add2.ConnectInput(1, c, 0);

        var result = graph.CreateNode<ResultNode>();
        result.ConnectInput(0, add2, 0);

        // Act
        var clonedGraph = graph.Clone();

        // Assert
        Assert.Equal(6, clonedGraph.Nodes.Count);

        // 実行して結果が同じことを確認
        await graph.CreateExecutor().ExecuteAsync();
        await clonedGraph.CreateExecutor().ExecuteAsync();

        var originalResult = graph.GetNodes<ResultNode>().First();
        var clonedResult = clonedGraph.GetNodes<ResultNode>().First();

        Assert.Equal(60, originalResult.Value);
        Assert.Equal(60, clonedResult.Value);
    }

    [Fact]
    public void CloneGraph_単一ノードのクローン()
    {
        // Arrange
        var graph = new Graph();
        var constant = graph.CreateNode<ConstantNode>();
        constant.SetValue(123);

        // Act
        Node[] nodes = [constant];
        var clonedGraph = Graph.Clone(nodes);

        // Assert
        Assert.Single(clonedGraph.Nodes);

        var clonedConstant = clonedGraph.GetNodes<ConstantNode>().First();
        Assert.NotSame(constant, clonedConstant);
        Assert.NotEqual(constant.Id, clonedConstant.Id);
    }

    [Fact]
    public void CloneGraph_空の配列でクローン()
    {
        // Arrange & Act
        var clonedGraph = Graph.Clone(Array.Empty<Node>());

        // Assert
        Assert.Empty(clonedGraph.Nodes);
    }
}
