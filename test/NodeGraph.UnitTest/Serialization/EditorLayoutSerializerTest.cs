using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Serialization;
using NodeGraph.Model;

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
            EditorLayoutSerializer.SaveLayout(editorGraph, tempFile);

            // 位置をリセット
            editorGraph.Nodes[0].X = 0;
            editorGraph.Nodes[0].Y = 0;
            editorGraph.Nodes[1].X = 0;
            editorGraph.Nodes[1].Y = 0;

            // Act - Load
            EditorLayoutSerializer.LoadLayout(tempFile, editorGraph);

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
        EditorLayoutSerializer.LoadLayout(nonExistentFile, editorGraph);
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
            EditorLayoutSerializer.SaveLayout(editorGraph, tempFile);

            // 位置をリセット
            foreach (var node in editorGraph.Nodes)
            {
                node.X = 0;
                node.Y = 0;
            }

            EditorLayoutSerializer.LoadLayout(tempFile, editorGraph);

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
                EditorLayoutSerializer.LoadLayout(tempFile, editorGraph));

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
            EditorLayoutSerializer.SaveLayout(editorGraph, tempFile);
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
            EditorLayoutSerializer.SaveLayout(editorGraph, tempFile);

            // 新しいEditorGraphを作成（同じGraphから）
            var newEditorGraph = new EditorGraph(graph, selectionManager);
            EditorLayoutSerializer.LoadLayout(tempFile, newEditorGraph);

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
            EditorLayoutSerializer.SaveLayout(editorGraph, tempFile);
            EditorLayoutSerializer.LoadLayout(tempFile, editorGraph);

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
            EditorLayoutSerializer.SaveLayout(editorGraph1, tempFile);

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
            EditorLayoutSerializer.LoadLayout(tempFile, editorGraph2);

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
            EditorLayoutSerializer.SaveLayout(editorGraph, tempFile);
            editorGraph.Nodes[0].X = 0;
            editorGraph.Nodes[0].Y = 0;
            EditorLayoutSerializer.LoadLayout(tempFile, editorGraph);

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
            EditorLayoutSerializer.SaveLayout(editorGraph, tempFile);
            editorGraph.Nodes[0].X = 0;
            editorGraph.Nodes[0].Y = 0;
            EditorLayoutSerializer.LoadLayout(tempFile, editorGraph);

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
