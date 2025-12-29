using NodeGraph.Editor.Services;
using NodeGraph.Model;
using Xunit;

namespace NodeGraph.UnitTest.Plugin;

/// <summary>
/// プラグインテスト用のフィクスチャ
/// テスト間でPluginServiceを共有し、プラグインを一度だけロードする
/// </summary>
public class PluginTestFixture : IDisposable
{
    public PluginService PluginService { get; }
    public string PluginsDirectory { get; }

    public PluginTestFixture()
    {
        PluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        PluginService = new PluginService();
        PluginService.LoadPlugins(PluginsDirectory);
    }

    public void Dispose()
    {
        // クリーンアップは不要（AssemblyLoadContextはアンロード不可）
    }
}

/// <summary>
/// プラグインテストのコレクション定義
/// このコレクションに属するテストは順次実行される
/// </summary>
[CollectionDefinition("PluginTests")]
public class PluginTestCollection : ICollectionFixture<PluginTestFixture>
{
}

/// <summary>
/// プラグインローディングの統合テスト
/// SamplePluginを実際にロードして、ノードが正しく発見・登録されることを確認
/// </summary>
[Collection("PluginTests")]
public class PluginIntegrationTest
{
    private readonly PluginTestFixture _fixture;

    public PluginIntegrationTest(PluginTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void LoadPlugins_WithSamplePlugin_DiscoversTwoNodes()
    {
        // Assert
        var successResults = _fixture.PluginService.LoadResults.Where(r => r.Success).ToList();
        Assert.Single(successResults); // SamplePlugin.dllが1つロードされる

        // 2つのノード（SampleDoubleNode, SampleReverseStringNode）が発見される
        Assert.Equal(2, _fixture.PluginService.PluginNodeTypes.Count);
    }

    [Fact]
    public void LoadPlugins_WithSamplePlugin_DiscoversSampleDoubleNode()
    {
        // Assert
        var doubleNodeType = _fixture.PluginService.PluginNodeTypes
            .FirstOrDefault(t => t.Name == "SampleDoubleNode");
        Assert.NotNull(doubleNodeType);
        Assert.True(typeof(Node).IsAssignableFrom(doubleNodeType));
    }

    [Fact]
    public void LoadPlugins_WithSamplePlugin_DiscoversSampleReverseStringNode()
    {
        // Assert
        var reverseNodeType = _fixture.PluginService.PluginNodeTypes
            .FirstOrDefault(t => t.Name == "SampleReverseStringNode");
        Assert.NotNull(reverseNodeType);
        Assert.True(typeof(Node).IsAssignableFrom(reverseNodeType));
    }

    [Fact]
    public void PluginNode_CanBeInstantiated()
    {
        // Act & Assert
        foreach (var nodeType in _fixture.PluginService.PluginNodeTypes)
        {
            var instance = Activator.CreateInstance(nodeType);
            Assert.NotNull(instance);
            Assert.IsAssignableFrom<Node>(instance);
        }
    }

    [Fact]
    public void PluginNode_HasCorrectDisplayNameAndDirectory()
    {
        // Act
        var doubleNodeType = _fixture.PluginService.PluginNodeTypes
            .First(t => t.Name == "SampleDoubleNode");
        var node = (Node)Activator.CreateInstance(doubleNodeType)!;

        // Assert
        Assert.Equal("Double", node.GetDisplayName());
        Assert.Equal("Sample Plugin", node.GetDirectory());
    }

    [Fact]
    public void PluginNode_HasCorrectPorts()
    {
        // Act
        var doubleNodeType = _fixture.PluginService.PluginNodeTypes
            .First(t => t.Name == "SampleDoubleNode");
        var node = (Node)Activator.CreateInstance(doubleNodeType)!;

        // Assert - 入力ポートと出力ポートが正しく生成されている
        Assert.Single(node.InputPorts);
        Assert.Single(node.OutputPorts);
        Assert.Single(node.ExecInPorts);
        Assert.Single(node.ExecOutPorts);
    }

    [Fact]
    public void NodeTypeService_RegistersPluginTypes()
    {
        // Arrange
        var nodeTypeService = new NodeTypeService();

        // Act
        nodeTypeService.RegisterPluginTypes(_fixture.PluginService.PluginNodeTypes);

        // Assert
        var doubleNode = nodeTypeService.NodeTypes
            .FirstOrDefault(n => n.DisplayName == "Double" && n.Directory == "Sample Plugin");
        var reverseNode = nodeTypeService.NodeTypes
            .FirstOrDefault(n => n.DisplayName == "Reverse String" && n.Directory == "Sample Plugin");

        Assert.NotNull(doubleNode);
        Assert.NotNull(reverseNode);
    }

    [Fact]
    public void NodeTypeService_Search_FindsPluginNodes()
    {
        // Arrange
        var nodeTypeService = new NodeTypeService();
        nodeTypeService.RegisterPluginTypes(_fixture.PluginService.PluginNodeTypes);

        // Act
        var searchResults = nodeTypeService.Search("Sample Plugin").ToList();

        // Assert
        Assert.Equal(2, searchResults.Count);
    }

    [Fact]
    public void PluginNode_CanConnectToBuiltInNodes()
    {
        // Arrange
        var doubleNodeType = _fixture.PluginService.PluginNodeTypes
            .First(t => t.Name == "SampleDoubleNode");
        var pluginNode = (Node)Activator.CreateInstance(doubleNodeType)!;

        var graph = new Graph();
        var constantNode = graph.CreateNode<FloatConstantNode>();
        var resultNode = graph.CreateNode<FloatResultNode>();

        // Act - プラグインノードのポートと組み込みノードのポートを接続
        var connected1 = constantNode.OutputPorts[0].Connect(pluginNode.InputPorts[0]);
        var connected2 = pluginNode.OutputPorts[0].Connect(resultNode.InputPorts[0]);

        // Assert
        Assert.True(connected1);
        Assert.True(connected2);
        Assert.Single(constantNode.OutputPorts[0].ConnectedPorts);
        Assert.Single(pluginNode.OutputPorts[0].ConnectedPorts);
    }

    [Fact]
    public void LoadPlugins_ReturnsCorrectLoadResult()
    {
        // Assert
        var samplePluginResult = _fixture.PluginService.LoadResults
            .FirstOrDefault(r => r.DllPath.EndsWith("NodeGraph.SamplePlugin.dll"));

        Assert.NotNull(samplePluginResult);
        Assert.True(samplePluginResult.Success);
        Assert.Null(samplePluginResult.ErrorMessage);
        Assert.Equal(2, samplePluginResult.DiscoveredNodeTypes.Count);
    }
}
