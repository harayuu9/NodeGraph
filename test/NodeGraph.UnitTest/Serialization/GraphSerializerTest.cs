using NodeGraph.Model;
using NodeGraph.Model.Serialization;

namespace NodeGraph.UnitTest.Serialization;

public class GraphSerializerTest
{
    [Fact]
    public void SaveAndLoad_SimpleGraph_PreservesStructure()
    {
        // Arrange
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        constant.SetPropertyValue("Value", 42.0f);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, constant, 0); // InputPorts[0] <- OutputPorts[0]

        var tempFile = Path.GetTempFileName() + ".graph.yml";

        try
        {
            // Act - Save
            GraphSerializer.SaveToYaml(graph, tempFile);

            // Assert - File exists
            Assert.True(File.Exists(tempFile));

            // Act - Load
            var loadedGraph = GraphSerializer.LoadFromYaml(tempFile);

            // Assert - Graph structure
            Assert.Equal(2, loadedGraph.Nodes.Count);

            var loadedConstant = loadedGraph.GetNodes<FloatConstantNode>().First();
            var loadedResult = loadedGraph.GetNodes<FloatResultNode>().First();

            // プロパティ値が保持されている
            var value = loadedConstant.GetPropertyValue("Value");
            Assert.Equal(42.0f, value);

            // 接続が保持されている
            var resultInput = (SingleConnectPort)loadedResult.InputPorts[0];
            Assert.NotNull(resultInput.ConnectedPort);
            Assert.Same(loadedConstant.OutputPorts[0], resultInput.ConnectedPort);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_ComplexGraph_PreservesAllConnections()
    {
        // Arrange
        var graph = new Graph();
        var a = graph.CreateNode<FloatConstantNode>();
        a.SetPropertyValue("Value", 10.0f);

        var b = graph.CreateNode<FloatConstantNode>();
        b.SetPropertyValue("Value", 5.0f);

        var add = graph.CreateNode<FloatAddNode>();
        add.ConnectInput(0, a, 0); // _a <- a.Output
        add.ConnectInput(1, b, 0); // _b <- b.Output

        var multiply = graph.CreateNode<FloatMultiplyNode>();
        multiply.ConnectInput(0, add, 0); // _a <- add.Result
        multiply.ConnectInput(1, b, 0);   // _b <- b.Output

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, multiply, 0); // _value <- multiply.Result

        var tempFile = Path.GetTempFileName() + ".graph.yml";

        try
        {
            // Act
            GraphSerializer.SaveToYaml(graph, tempFile);
            var loadedGraph = GraphSerializer.LoadFromYaml(tempFile);

            // Assert
            Assert.Equal(5, loadedGraph.Nodes.Count);

            var loadedA = loadedGraph.GetNodes<FloatConstantNode>().First(n =>
                Math.Abs((float)n.GetPropertyValue("Value")! - 10.0f) < 0.001f);
            var loadedB = loadedGraph.GetNodes<FloatConstantNode>().First(n =>
                Math.Abs((float)n.GetPropertyValue("Value")! - 5.0f) < 0.001f);
            var loadedAdd = loadedGraph.GetNodes<FloatAddNode>().First();
            var loadedMultiply = loadedGraph.GetNodes<FloatMultiplyNode>().First();
            var loadedResult = loadedGraph.GetNodes<FloatResultNode>().First();

            // 接続を検証
            var addInputA = (SingleConnectPort)loadedAdd.InputPorts[0];
            var addInputB = (SingleConnectPort)loadedAdd.InputPorts[1];
            var multiplyInputA = (SingleConnectPort)loadedMultiply.InputPorts[0];
            var multiplyInputB = (SingleConnectPort)loadedMultiply.InputPorts[1];
            var resultInput = (SingleConnectPort)loadedResult.InputPorts[0];

            Assert.Same(loadedA.OutputPorts[0], addInputA.ConnectedPort);
            Assert.Same(loadedB.OutputPorts[0], addInputB.ConnectedPort);
            Assert.Same(loadedAdd.OutputPorts[0], multiplyInputA.ConnectedPort);
            Assert.Same(loadedB.OutputPorts[0], multiplyInputB.ConnectedPort);
            Assert.Same(loadedMultiply.OutputPorts[0], resultInput.ConnectedPort);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task SaveAndLoad_GraphExecution_ProducesSameResult()
    {
        // Arrange
        var graph = new Graph();
        var a = graph.CreateNode<FloatConstantNode>();
        a.SetPropertyValue("Value", 7.0f);

        var b = graph.CreateNode<FloatConstantNode>();
        b.SetPropertyValue("Value", 3.0f);

        var subtract = graph.CreateNode<FloatSubtractNode>();
        subtract.ConnectInput(0, a, 0);
        subtract.ConnectInput(1, b, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, subtract, 0);

        // 元のグラフを実行
        var executor1 = graph.CreateExecutor();
        await executor1.ExecuteAsync();
        var originalResult = result.Value;

        var tempFile = Path.GetTempFileName() + ".graph.yml";

        try
        {
            // Act - シリアライズしてデシリアライズ
            GraphSerializer.SaveToYaml(graph, tempFile);
            var loadedGraph = GraphSerializer.LoadFromYaml(tempFile);

            // ロードしたグラフを実行
            var loadedResult = loadedGraph.GetNodes<FloatResultNode>().First();
            var executor2 = loadedGraph.CreateExecutor();
            await executor2.ExecuteAsync();

            // Assert - 同じ結果が得られる
            Assert.Equal(originalResult, loadedResult.Value);
            Assert.Equal(4.0f, loadedResult.Value); // 7 - 3 = 4
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_AllNodeTypes_Success()
    {
        // Arrange
        var graph = new Graph();
        var floatConst = graph.CreateNode<FloatConstantNode>();
        floatConst.SetPropertyValue("Value", 1.5f);

        var intConst = graph.CreateNode<IntConstantNode>();
        intConst.SetPropertyValue("Value", 42);

        var stringConst = graph.CreateNode<StringConstantNode>();
        stringConst.SetPropertyValue("Value", "Hello");

        var add = graph.CreateNode<FloatAddNode>();
        var subtract = graph.CreateNode<FloatSubtractNode>();
        var multiply = graph.CreateNode<FloatMultiplyNode>();
        var divide = graph.CreateNode<FloatDivideNode>();
        var result = graph.CreateNode<FloatResultNode>();
        var preview = graph.CreateNode<PreviewNode>();

        var tempFile = Path.GetTempFileName() + ".graph.yml";

        try
        {
            // Act
            GraphSerializer.SaveToYaml(graph, tempFile);
            var loadedGraph = GraphSerializer.LoadFromYaml(tempFile);

            // Assert - すべてのノードタイプが復元される
            Assert.Single(loadedGraph.GetNodes<FloatConstantNode>());
            Assert.Single(loadedGraph.GetNodes<IntConstantNode>());
            Assert.Single(loadedGraph.GetNodes<StringConstantNode>());
            Assert.Single(loadedGraph.GetNodes<FloatAddNode>());
            Assert.Single(loadedGraph.GetNodes<FloatSubtractNode>());
            Assert.Single(loadedGraph.GetNodes<FloatMultiplyNode>());
            Assert.Single(loadedGraph.GetNodes<FloatDivideNode>());
            Assert.Single(loadedGraph.GetNodes<FloatResultNode>());
            Assert.Single(loadedGraph.GetNodes<PreviewNode>());

            // プロパティ値の検証
            var loadedFloat = loadedGraph.GetNodes<FloatConstantNode>().First();
            Assert.Equal(1.5f, loadedFloat.GetPropertyValue("Value"));

            var loadedInt = loadedGraph.GetNodes<IntConstantNode>().First();
            Assert.Equal(42, loadedInt.GetPropertyValue("Value"));

            var loadedString = loadedGraph.GetNodes<StringConstantNode>().First();
            Assert.Equal("Hello", loadedString.GetPropertyValue("Value"));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_PortIds_ArePreserved()
    {
        // Arrange
        var graph = new Graph();
        var node = graph.CreateNode<FloatAddNode>();

        var originalInputAId = node.InputPorts[0].Id;
        var originalInputBId = node.InputPorts[1].Id;
        var originalOutputId = node.OutputPorts[0].Id;

        var tempFile = Path.GetTempFileName() + ".graph.yml";

        try
        {
            // Act
            GraphSerializer.SaveToYaml(graph, tempFile);
            var loadedGraph = GraphSerializer.LoadFromYaml(tempFile);

            var loadedNode = loadedGraph.GetNodes<FloatAddNode>().First();

            // Assert - ポートIDが保持される
            Assert.Equal(originalInputAId, loadedNode.InputPorts[0].Id);
            Assert.Equal(originalInputBId, loadedNode.InputPorts[1].Id);
            Assert.Equal(originalOutputId, loadedNode.OutputPorts[0].Id);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadFromYaml_InvalidNodeType_ThrowsException()
    {
        // Arrange
        var yaml = @"
version: 1.0.0
nodes:
  - id: 550e8400-e29b-41d4-a716-446655440000
    type: NodeGraph.Model.NonExistentNode
    properties: {}
    ports: []
connections: []
";
        var tempFile = Path.GetTempFileName() + ".graph.yml";
        File.WriteAllText(tempFile, yaml);

        try
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                GraphSerializer.LoadFromYaml(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadFromYaml_IncompatibleMajorVersion_ThrowsException()
    {
        // Arrange
        var yaml = @"
version: 2.0.0
nodes: []
connections: []
";
        var tempFile = Path.GetTempFileName() + ".graph.yml";
        File.WriteAllText(tempFile, yaml);

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                GraphSerializer.LoadFromYaml(tempFile));

            Assert.Contains("Incompatible file version", ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SaveToYaml_ProducesReadableYaml()
    {
        // Arrange
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        constant.SetPropertyValue("Value", 123.45f);

        var tempFile = Path.GetTempFileName() + ".graph.yml";

        try
        {
            // Act
            GraphSerializer.SaveToYaml(graph, tempFile);
            var yamlContent = File.ReadAllText(tempFile);

            // Assert - YAMLが読みやすい形式になっている
            Assert.Contains("version:", yamlContent);
            Assert.Contains("nodes:", yamlContent);
            Assert.Contains("FloatConstantNode", yamlContent);
            Assert.Contains("123.45", yamlContent);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
