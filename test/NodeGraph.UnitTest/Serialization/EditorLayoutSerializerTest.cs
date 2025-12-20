using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Serialization;
using NodeGraph.Model;
using NodeGraph.Model.Serialization;

namespace NodeGraph.UnitTest.Serialization;

public class EditorLayoutSerializerTest
{
    [Fact]
    public void SaveAndLoad_NodePositions_ArePreserved()
    {
        // Arrange
        var graph = new Graph();
        var node1 = graph.CreateNode<FloatConstantNode>();
        var node2 = graph.CreateNode<FloatAddNode>();

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        // ノード位置を設定
        editorGraph.Nodes[0].X = 100.0;
        editorGraph.Nodes[0].Y = 200.0;
        editorGraph.Nodes[1].X = 400.0;
        editorGraph.Nodes[1].Y = 300.0;

        var tempFile = Path.GetTempFileName() + ".layout.yml";

        try
        {
            // Act - Save
            EditorLayoutSerializer.SaveLayoutToFile(editorGraph, tempFile);

            // 位置をリセット
            editorGraph.Nodes[0].X = 0;
            editorGraph.Nodes[0].Y = 0;
            editorGraph.Nodes[1].X = 0;
            editorGraph.Nodes[1].Y = 0;

            // Act - Load
            EditorLayoutSerializer.LoadLayoutFromFile(tempFile, editorGraph);

            // Assert - 位置が復元される
            Assert.Equal(100.0, editorGraph.Nodes[0].X);
            Assert.Equal(200.0, editorGraph.Nodes[0].Y);
            Assert.Equal(400.0, editorGraph.Nodes[1].X);
            Assert.Equal(300.0, editorGraph.Nodes[1].Y);
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
    public void LoadLayout_FileNotExists_DoesNotThrow()
    {
        // Arrange
        var graph = new Graph();
        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".layout.yml");

        // Act & Assert - 例外が発生しない
        EditorLayoutSerializer.LoadLayoutFromFile(nonExistentFile, editorGraph);
    }

    [Fact]
    public void SaveAndLoad_ManyNodes_AllPositionsPreserved()
    {
        // Arrange
        var graph = new Graph();
        var random = new Random(42); // 再現性のためシード固定

        // 10個のノードを作成
        for (int i = 0; i < 10; i++)
        {
            graph.CreateNode<FloatConstantNode>();
        }

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        // ランダムな位置を設定
        var expectedPositions = new List<(double X, double Y)>();
        foreach (var node in editorGraph.Nodes)
        {
            node.X = random.NextDouble() * 1000;
            node.Y = random.NextDouble() * 1000;
            expectedPositions.Add((node.X, node.Y));
        }

        var tempFile = Path.GetTempFileName() + ".layout.yml";

        try
        {
            // Act
            EditorLayoutSerializer.SaveLayoutToFile(editorGraph, tempFile);

            // 位置をリセット
            foreach (var node in editorGraph.Nodes)
            {
                node.X = 0;
                node.Y = 0;
            }

            EditorLayoutSerializer.LoadLayoutFromFile(tempFile, editorGraph);

            // Assert
            for (int i = 0; i < editorGraph.Nodes.Count; i++)
            {
                Assert.Equal(expectedPositions[i].X, editorGraph.Nodes[i].X, precision: 5);
                Assert.Equal(expectedPositions[i].Y, editorGraph.Nodes[i].Y, precision: 5);
            }
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
    public void LoadLayout_IncompatibleMajorVersion_ThrowsException()
    {
        // Arrange
        var yaml = @"
version: 2.0.0
nodes: {}
";
        var tempFile = Path.GetTempFileName() + ".layout.yml";
        File.WriteAllText(tempFile, yaml);

        var graph = new Graph();
        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                EditorLayoutSerializer.LoadLayoutFromFile(tempFile, editorGraph));

            Assert.Contains("Incompatible layout version", ex.Message);
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
    public void SaveLayout_ProducesReadableYaml()
    {
        // Arrange
        var graph = new Graph();
        graph.CreateNode<FloatConstantNode>();

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);
        editorGraph.Nodes[0].X = 123.45;
        editorGraph.Nodes[0].Y = 678.90;

        var tempFile = Path.GetTempFileName() + ".layout.yml";

        try
        {
            // Act
            EditorLayoutSerializer.SaveLayoutToFile(editorGraph, tempFile);
            var yamlContent = File.ReadAllText(tempFile);

            // Assert - YAMLが読みやすい形式になっている
            Assert.Contains("version:", yamlContent);
            Assert.Contains("nodes:", yamlContent);
            Assert.Contains("123.45", yamlContent);
            Assert.Contains("678.9", yamlContent); // YAMLは末尾の0を省略する
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
    public void SaveAndLoad_NodeIdMapping_IsCorrect()
    {
        // Arrange
        var graph = new Graph();
        var node1 = graph.CreateNode<FloatConstantNode>();
        var node2 = graph.CreateNode<FloatAddNode>();
        var node3 = graph.CreateNode<FloatMultiplyNode>();

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        // 各ノードに異なる位置を設定
        editorGraph.Nodes[0].X = 10.0;
        editorGraph.Nodes[0].Y = 20.0;
        editorGraph.Nodes[1].X = 30.0;
        editorGraph.Nodes[1].Y = 40.0;
        editorGraph.Nodes[2].X = 50.0;
        editorGraph.Nodes[2].Y = 60.0;

        var tempFile = Path.GetTempFileName() + ".layout.yml";

        try
        {
            // Act
            EditorLayoutSerializer.SaveLayoutToFile(editorGraph, tempFile);

            // 新しいEditorGraphを作成（同じGraphから）
            var newEditorGraph = new EditorGraph(graph, selectionManager);
            EditorLayoutSerializer.LoadLayoutFromFile(tempFile, newEditorGraph);

            // Assert - NodeIdでマッピングされるので、正しい位置が復元される
            var editorNode1 = newEditorGraph.Nodes.First(n => n.Node == node1);
            var editorNode2 = newEditorGraph.Nodes.First(n => n.Node == node2);
            var editorNode3 = newEditorGraph.Nodes.First(n => n.Node == node3);

            Assert.Equal(10.0, editorNode1.X);
            Assert.Equal(20.0, editorNode1.Y);
            Assert.Equal(30.0, editorNode2.X);
            Assert.Equal(40.0, editorNode2.Y);
            Assert.Equal(50.0, editorNode3.X);
            Assert.Equal(60.0, editorNode3.Y);
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
    public void SaveAndLoad_EmptyGraph_DoesNotThrow()
    {
        // Arrange
        var graph = new Graph();
        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        var tempFile = Path.GetTempFileName() + ".layout.yml";

        try
        {
            // Act & Assert - 空のグラフでも例外が発生しない
            EditorLayoutSerializer.SaveLayoutToFile(editorGraph, tempFile);
            EditorLayoutSerializer.LoadLayoutFromFile(tempFile, editorGraph);

            Assert.True(File.Exists(tempFile));
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
    public void LoadLayout_PartialNodeMatch_LoadsAvailablePositions()
    {
        // Arrange
        var graph1 = new Graph();
        var node1 = graph1.CreateNode<FloatConstantNode>();
        var node2 = graph1.CreateNode<FloatAddNode>();

        var selectionManager = new SelectionManager();
        var editorGraph1 = new EditorGraph(graph1, selectionManager);
        editorGraph1.Nodes[0].X = 100.0;
        editorGraph1.Nodes[0].Y = 200.0;
        editorGraph1.Nodes[1].X = 300.0;
        editorGraph1.Nodes[1].Y = 400.0;

        var tempFile = Path.GetTempFileName() + ".layout.yml";

        try
        {
            // レイアウトを保存
            EditorLayoutSerializer.SaveLayoutToFile(editorGraph1, tempFile);

            // 新しいグラフを作成（node1のみ同じNodeIdで再作成）
            var graph2 = new Graph();
            var node1Copy = graph2.CreateNode<FloatConstantNode>();
            var node3 = graph2.CreateNode<FloatMultiplyNode>();

            // node1のIDを強制的に同じにする（リフレクション）
            var idField = typeof(Node).GetProperty("Id")!
                .GetBackingField();
            idField!.SetValue(node1Copy, node1.Id);

            var editorGraph2 = new EditorGraph(graph2, selectionManager);

            // Act - 部分的にマッチするレイアウトを読み込む
            EditorLayoutSerializer.LoadLayoutFromFile(tempFile, editorGraph2);

            // Assert - node1の位置は復元されるが、node3はデフォルト位置のまま
            var editorNode1 = editorGraph2.Nodes.First(n => n.Node == node1Copy);
            var editorNode3 = editorGraph2.Nodes.First(n => n.Node == node3);

            Assert.Equal(100.0, editorNode1.X);
            Assert.Equal(200.0, editorNode1.Y);
            Assert.Equal(0.0, editorNode3.X); // デフォルト
            Assert.Equal(0.0, editorNode3.Y); // デフォルト
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
    public void SaveLayout_NegativePositions_ArePreserved()
    {
        // Arrange
        var graph = new Graph();
        graph.CreateNode<FloatConstantNode>();

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);
        editorGraph.Nodes[0].X = -100.5;
        editorGraph.Nodes[0].Y = -200.75;

        var tempFile = Path.GetTempFileName() + ".layout.yml";

        try
        {
            // Act
            EditorLayoutSerializer.SaveLayoutToFile(editorGraph, tempFile);
            editorGraph.Nodes[0].X = 0;
            editorGraph.Nodes[0].Y = 0;
            EditorLayoutSerializer.LoadLayoutFromFile(tempFile, editorGraph);

            // Assert - 負の座標も正しく保存・復元される
            Assert.Equal(-100.5, editorGraph.Nodes[0].X);
            Assert.Equal(-200.75, editorGraph.Nodes[0].Y);
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
    public void SaveLayout_LargeCoordinates_ArePreserved()
    {
        // Arrange
        var graph = new Graph();
        graph.CreateNode<FloatConstantNode>();

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);
        editorGraph.Nodes[0].X = 999999.999;
        editorGraph.Nodes[0].Y = 888888.888;

        var tempFile = Path.GetTempFileName() + ".layout.yml";

        try
        {
            // Act
            EditorLayoutSerializer.SaveLayoutToFile(editorGraph, tempFile);
            editorGraph.Nodes[0].X = 0;
            editorGraph.Nodes[0].Y = 0;
            EditorLayoutSerializer.LoadLayoutFromFile(tempFile, editorGraph);

            // Assert - 大きな座標も正しく保存・復元される
            Assert.Equal(999999.999, editorGraph.Nodes[0].X, precision: 3);
            Assert.Equal(888888.888, editorGraph.Nodes[0].Y, precision: 3);
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
    public void FullSaveLoad_GraphAndLayout_BothAreRestored()
    {
        // Arrange
        var graph = new Graph();
        var constant1 = graph.CreateNode<FloatConstantNode>();
        constant1.SetPropertyValue("Value", 10.0f);

        var constant2 = graph.CreateNode<FloatConstantNode>();
        constant2.SetPropertyValue("Value", 5.0f);

        var add = graph.CreateNode<FloatAddNode>();
        add.ConnectInput(0, constant1, 0);
        add.ConnectInput(1, constant2, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, add, 0);

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        // ノード位置を設定
        editorGraph.Nodes[0].X = 100.0;
        editorGraph.Nodes[0].Y = 150.0;
        editorGraph.Nodes[1].X = 100.0;
        editorGraph.Nodes[1].Y = 250.0;
        editorGraph.Nodes[2].X = 400.0;
        editorGraph.Nodes[2].Y = 200.0;
        editorGraph.Nodes[3].X = 700.0;
        editorGraph.Nodes[3].Y = 200.0;

        var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Act - EditorGraph.Save を使って保存
            editorGraph.Save(basePath);

            // 新しいEditorGraphとして読み込む
            var loadedEditorGraph = EditorGraph.Load(basePath, selectionManager);

            // Assert - グラフの構造が復元されている
            Assert.Equal(4, loadedEditorGraph.Nodes.Count);
            Assert.Equal(3, loadedEditorGraph.Connections.Count);

            // ノードタイプが復元されている
            Assert.Equal(2, loadedEditorGraph.Graph.GetNodes<FloatConstantNode>().Length);
            Assert.Single(loadedEditorGraph.Graph.GetNodes<FloatAddNode>());
            Assert.Single(loadedEditorGraph.Graph.GetNodes<FloatResultNode>());

            // プロパティ値が復元されている
            var loadedConstants = loadedEditorGraph.Graph.GetNodes<FloatConstantNode>();
            var values = loadedConstants.Select(n => (float)n.GetPropertyValue("Value")!).OrderBy(v => v).ToArray();
            Assert.Equal(5.0f, values[0]);
            Assert.Equal(10.0f, values[1]);

            // 接続が復元されている
            var loadedAdd = loadedEditorGraph.Graph.GetNodes<FloatAddNode>()[0];
            var loadedResult = loadedEditorGraph.Graph.GetNodes<FloatResultNode>()[0];
            Assert.NotNull(((SingleConnectPort)loadedAdd.InputPorts[0]).ConnectedPort);
            Assert.NotNull(((SingleConnectPort)loadedAdd.InputPorts[1]).ConnectedPort);
            Assert.NotNull(((SingleConnectPort)loadedResult.InputPorts[0]).ConnectedPort);

            // レイアウトが復元されている
            var loadedConstant1 = loadedEditorGraph.Nodes.First(n =>
                n.Node.GetType() == typeof(FloatConstantNode) &&
                Math.Abs((float)n.Node.GetPropertyValue("Value")! - 10.0f) < 0.001f);
            var loadedConstant2 = loadedEditorGraph.Nodes.First(n =>
                n.Node.GetType() == typeof(FloatConstantNode) &&
                Math.Abs((float)n.Node.GetPropertyValue("Value")! - 5.0f) < 0.001f);
            var loadedAddNode = loadedEditorGraph.Nodes.First(n => n.Node.GetType() == typeof(FloatAddNode));
            var loadedResultNode = loadedEditorGraph.Nodes.First(n => n.Node.GetType() == typeof(FloatResultNode));

            Assert.Equal(100.0, loadedConstant1.X);
            Assert.Equal(150.0, loadedConstant1.Y);
            Assert.Equal(100.0, loadedConstant2.X);
            Assert.Equal(250.0, loadedConstant2.Y);
            Assert.Equal(400.0, loadedAddNode.X);
            Assert.Equal(200.0, loadedAddNode.Y);
            Assert.Equal(700.0, loadedResultNode.X);
            Assert.Equal(200.0, loadedResultNode.Y);
        }
        finally
        {
            // Cleanup
            var graphPath = Path.ChangeExtension(basePath, ".graph.yml");
            var layoutPath = Path.ChangeExtension(basePath, ".layout.yml");
            if (File.Exists(graphPath)) File.Delete(graphPath);
            if (File.Exists(layoutPath)) File.Delete(layoutPath);
        }
    }

    [Fact]
    public async Task FullSaveLoad_ExecutionResultsMatch()
    {
        // Arrange
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();

        var a = graph.CreateNode<FloatConstantNode>();
        a.SetPropertyValue("Value", 7.0f);

        var b = graph.CreateNode<FloatConstantNode>();
        b.SetPropertyValue("Value", 3.0f);

        var multiply = graph.CreateNode<FloatMultiplyNode>();
        multiply.ConnectInput(0, a, 0);
        multiply.ConnectInput(1, b, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, multiply, 0);

        // Exec接続: Start → Multiply → Result
        start.ExecOutPorts[0].Connect(multiply.ExecInPorts[0]);
        multiply.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        // ノード位置を設定 (StartNode追加のためインデックスが1つずれる)
        editorGraph.Nodes[0].X = 0.0;   // Start
        editorGraph.Nodes[0].Y = 150.0;
        editorGraph.Nodes[1].X = 50.0;  // a
        editorGraph.Nodes[1].Y = 100.0;
        editorGraph.Nodes[2].X = 50.0;  // b
        editorGraph.Nodes[2].Y = 200.0;
        editorGraph.Nodes[3].X = 300.0; // multiply
        editorGraph.Nodes[3].Y = 150.0;
        editorGraph.Nodes[4].X = 550.0; // result
        editorGraph.Nodes[4].Y = 150.0;

        // 元のグラフを実行
        var executor1 = graph.CreateExecutor();
        await executor1.ExecuteAsync();
        var originalResult = result.Value;

        var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Act - 保存して読み込む
            editorGraph.Save(basePath);
            var loadedEditorGraph = EditorGraph.Load(basePath, selectionManager);

            // ロードしたグラフを実行
            var loadedResult = loadedEditorGraph.Graph.GetNodes<FloatResultNode>()[0];
            var executor2 = loadedEditorGraph.Graph.CreateExecutor();
            await executor2.ExecuteAsync();

            // Assert - 同じ結果が得られる
            Assert.Equal(originalResult, loadedResult.Value);
            Assert.Equal(21.0f, loadedResult.Value); // 7 * 3 = 21

            // レイアウトも復元されている
            var loadedMultiply = loadedEditorGraph.Nodes.First(n => n.Node.GetType() == typeof(FloatMultiplyNode));
            Assert.Equal(300.0, loadedMultiply.X);
            Assert.Equal(150.0, loadedMultiply.Y);
        }
        finally
        {
            var graphPath = Path.ChangeExtension(basePath, ".graph.yml");
            var layoutPath = Path.ChangeExtension(basePath, ".layout.yml");
            if (File.Exists(graphPath)) File.Delete(graphPath);
            if (File.Exists(layoutPath)) File.Delete(layoutPath);
        }
    }

    [Fact]
    public void FullSaveLoad_WithoutLayoutFile_GraphStillLoads()
    {
        // Arrange
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        constant.SetPropertyValue("Value", 42.0f);

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);
        editorGraph.Nodes[0].X = 123.0;
        editorGraph.Nodes[0].Y = 456.0;

        var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Act - Graphのみ保存してLayoutを削除
            editorGraph.Save(basePath);
            var layoutPath = Path.ChangeExtension(basePath, ".layout.yml");
            File.Delete(layoutPath);

            // Layoutファイルなしで読み込む
            var loadedEditorGraph = EditorGraph.Load(basePath, selectionManager);

            // Assert - グラフは読み込まれるが、レイアウトはデフォルト
            Assert.Single(loadedEditorGraph.Nodes);
            var loadedConstant = loadedEditorGraph.Graph.GetNodes<FloatConstantNode>()[0];
            Assert.Equal(42.0f, loadedConstant.GetPropertyValue("Value"));

            // レイアウトはデフォルト（0, 0）
            Assert.Equal(0.0, loadedEditorGraph.Nodes[0].X);
            Assert.Equal(0.0, loadedEditorGraph.Nodes[0].Y);
        }
        finally
        {
            var graphPath = Path.ChangeExtension(basePath, ".graph.yml");
            var layoutPath = Path.ChangeExtension(basePath, ".layout.yml");
            if (File.Exists(graphPath)) File.Delete(graphPath);
            if (File.Exists(layoutPath)) File.Delete(layoutPath);
        }
    }
}

// リフレクションヘルパー拡張メソッド
internal static class PropertyInfoExtensions
{
    public static System.Reflection.FieldInfo? GetBackingField(this System.Reflection.PropertyInfo property)
    {
        var backingFieldName = $"<{property.Name}>k__BackingField";
        return property.DeclaringType?.GetField(backingFieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    }
}
