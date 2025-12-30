using NodeGraph.App.Services;
using NodeGraph.Model;

namespace NodeGraph.UnitTest.Editor;

/// <summary>
/// NodeTypeServiceのテスト
/// </summary>
public class NodeTypeServiceTest
{
    [Fact]
    public void LoadNodeTypes_FindsAllNodeTypes()
    {
        var service = new NodeTypeService();

        // 少なくとも基本的なノードタイプが検出されることを確認
        Assert.NotEmpty(service.NodeTypes);

        // 特定のノードタイプが検出されていることを確認
        var nodeTypeNames = service.NodeTypes.Select(n => n.NodeType.Name).ToList();
        Assert.Contains("FloatConstantNode", nodeTypeNames);
        Assert.Contains("FloatAddNode", nodeTypeNames);
        Assert.Contains("StartNode", nodeTypeNames);
    }

    [Fact]
    public void Search_FiltersByDisplayName()
    {
        var service = new NodeTypeService();

        var results = service.Search("Constant").ToList();

        Assert.NotEmpty(results);
        Assert.All(results, r =>
            Assert.True(
                r.DisplayName.Contains("Constant", StringComparison.OrdinalIgnoreCase) ||
                r.Directory.Contains("Constant", StringComparison.OrdinalIgnoreCase) ||
                r.FullPath.Contains("Constant", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        var service = new NodeTypeService();

        var lowerResults = service.Search("constant").ToList();
        var upperResults = service.Search("CONSTANT").ToList();
        var mixedResults = service.Search("CoNsTaNt").ToList();

        Assert.Equal(lowerResults.Count, upperResults.Count);
        Assert.Equal(lowerResults.Count, mixedResults.Count);
    }

    [Fact]
    public void Search_EmptyString_ReturnsAllNodes()
    {
        var service = new NodeTypeService();

        var allNodes = service.NodeTypes;
        var searchResults = service.Search("").ToList();

        Assert.Equal(allNodes.Count, searchResults.Count);
    }

    [Fact]
    public void GetGroupedByDirectory_GroupsCorrectly()
    {
        var service = new NodeTypeService();

        var grouped = service.GetGroupedByDirectory().ToList();

        Assert.NotEmpty(grouped);

        // 各グループ内のアイテムが同じディレクトリを持つことを確認
        foreach (var group in grouped)
        {
            Assert.All(group, item => Assert.Equal(group.Key, item.Directory));
        }
    }

    [Fact]
    public void NodeTypeInfo_FullPath_FormatsCorrectly()
    {
        var service = new NodeTypeService();

        // ディレクトリがあるノードのFullPathをテスト
        var floatNode = service.NodeTypes.FirstOrDefault(n => n.NodeType == typeof(FloatConstantNode));
        if (floatNode != null && !string.IsNullOrEmpty(floatNode.Directory))
        {
            Assert.Equal($"{floatNode.Directory}/{floatNode.DisplayName}", floatNode.FullPath);
        }
    }

    [Fact]
    public void NodeTypes_AreSortedByDirectoryAndDisplayName()
    {
        var service = new NodeTypeService();

        var nodeTypes = service.NodeTypes.ToList();

        // ソート順を確認
        for (int i = 1; i < nodeTypes.Count; i++)
        {
            var prev = nodeTypes[i - 1];
            var curr = nodeTypes[i];

            var dirComparison = string.Compare(prev.Directory, curr.Directory, StringComparison.Ordinal);
            if (dirComparison == 0)
            {
                // 同じディレクトリ内では表示名でソート
                Assert.True(
                    string.Compare(prev.DisplayName, curr.DisplayName, StringComparison.Ordinal) <= 0,
                    $"Expected {prev.DisplayName} <= {curr.DisplayName} within directory {prev.Directory}");
            }
            else
            {
                // ディレクトリ順でソート
                Assert.True(dirComparison <= 0,
                    $"Expected directory {prev.Directory} <= {curr.Directory}");
            }
        }
    }
}
