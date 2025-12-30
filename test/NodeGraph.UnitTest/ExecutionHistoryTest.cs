using NodeGraph.App.Models;
using NodeGraph.App.Selection;
using NodeGraph.App.Serialization;
using NodeGraph.Model;
using Xunit.Abstractions;

namespace NodeGraph.UnitTest;

/// <summary>
/// 実行履歴の順序に関するテスト
/// </summary>
[Collection("Sequential")]
public class ExecutionHistoryTest
{
    private readonly ITestOutputHelper _output;

    public ExecutionHistoryTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 直列依存関係のあるノードは、トポロジカル順序で履歴に記録される
    /// A → B → C の場合、履歴は A, B, C の順序になるべき
    /// </summary>
    [Fact]
    public async Task SequentialNodes_RecordedInTopologicalOrder()
    {
        // Arrange: A → B → C の直列グラフを構築
        var graph = new Graph();

        var nodeA = graph.CreateNode<ConstantNode>();
        nodeA.SetValue(10);

        var nodeB = graph.CreateNode<AddNode>();
        nodeB.ConnectInput(0, nodeA, 0); // B depends on A
        nodeB.ConnectInput(1, nodeA, 0);

        var nodeC = graph.CreateNode<ResultNode>();
        nodeC.ConnectInput(0, nodeB, 0); // C depends on B

        // 履歴を記録
        var executionOrder = new List<NodeId>();
        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(
            onExecute: null,
            onExecuted: node => executionOrder.Add(node.Id)
        );

        // ログ出力
        _output.WriteLine("Execution order:");
        for (var i = 0; i < executionOrder.Count; i++)
        {
            var nodeId = executionOrder[i];
            var node = graph.Nodes.First(n => n.Id == nodeId);
            _output.WriteLine($"  {i + 1}. {node.GetType().Name} (Id: {nodeId})");
        }

        // Assert: A → B → C の順序で記録されているはず
        Assert.Equal(3, executionOrder.Count);

        var aIndex = executionOrder.IndexOf(nodeA.Id);
        var bIndex = executionOrder.IndexOf(nodeB.Id);
        var cIndex = executionOrder.IndexOf(nodeC.Id);

        Assert.True(aIndex < bIndex, "A should be recorded before B");
        Assert.True(bIndex < cIndex, "B should be recorded before C");
    }

    /// <summary>
    /// 複数の独立した入力ノードがある場合、その順序は実行完了順になる（非決定的）
    /// 依存ノードは必ず入力ノードの後に来る
    /// </summary>
    [Fact]
    public async Task IndependentInputNodes_DependentNodeComesAfter()
    {
        // Arrange: A, B (独立) → C の形
        //   A ──┐
        //       ├──→ C
        //   B ──┘
        var graph = new Graph();

        var nodeA = graph.CreateNode<ConstantNode>();
        nodeA.SetValue(10);

        var nodeB = graph.CreateNode<ConstantNode>();
        nodeB.SetValue(20);

        var nodeC = graph.CreateNode<AddNode>();
        nodeC.ConnectInput(0, nodeA, 0); // C depends on A
        nodeC.ConnectInput(1, nodeB, 0); // C depends on B

        // 履歴を記録
        var executionOrder = new List<NodeId>();
        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(
            onExecute: null,
            onExecuted: node => executionOrder.Add(node.Id)
        );

        // ログ出力
        _output.WriteLine("Execution order:");
        for (var i = 0; i < executionOrder.Count; i++)
        {
            var nodeId = executionOrder[i];
            var node = graph.Nodes.First(n => n.Id == nodeId);
            _output.WriteLine($"  {i + 1}. {node.GetType().Name} (Id: {nodeId})");
        }

        // Assert: C は A と B の両方より後に来るはず
        Assert.Equal(3, executionOrder.Count);

        var aIndex = executionOrder.IndexOf(nodeA.Id);
        var bIndex = executionOrder.IndexOf(nodeB.Id);
        var cIndex = executionOrder.IndexOf(nodeC.Id);

        Assert.True(aIndex < cIndex, "A should be recorded before C");
        Assert.True(bIndex < cIndex, "B should be recorded before C");
    }

    /// <summary>
    /// ダイヤモンド依存関係のテスト
    ///     A
    ///    / \
    ///   B   C
    ///    \ /
    ///     D
    /// </summary>
    [Fact]
    public async Task DiamondDependency_CorrectOrder()
    {
        var graph = new Graph();

        var nodeA = graph.CreateNode<ConstantNode>();
        nodeA.SetValue(10);

        var nodeB = graph.CreateNode<AddNode>();
        nodeB.ConnectInput(0, nodeA, 0);
        nodeB.ConnectInput(1, nodeA, 0);

        var nodeC = graph.CreateNode<AddNode>();
        nodeC.ConnectInput(0, nodeA, 0);
        nodeC.ConnectInput(1, nodeA, 0);

        var nodeD = graph.CreateNode<AddNode>();
        nodeD.ConnectInput(0, nodeB, 0);
        nodeD.ConnectInput(1, nodeC, 0);

        var executionOrder = new List<NodeId>();
        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(
            onExecute: null,
            onExecuted: node => executionOrder.Add(node.Id)
        );

        _output.WriteLine("Execution order:");
        for (var i = 0; i < executionOrder.Count; i++)
        {
            var nodeId = executionOrder[i];
            var node = graph.Nodes.First(n => n.Id == nodeId);
            _output.WriteLine($"  {i + 1}. {node.GetType().Name} (Id: {nodeId})");
        }

        Assert.Equal(4, executionOrder.Count);

        var aIndex = executionOrder.IndexOf(nodeA.Id);
        var bIndex = executionOrder.IndexOf(nodeB.Id);
        var cIndex = executionOrder.IndexOf(nodeC.Id);
        var dIndex = executionOrder.IndexOf(nodeD.Id);

        // A は B, C より先
        Assert.True(aIndex < bIndex, "A should be before B");
        Assert.True(aIndex < cIndex, "A should be before C");

        // B, C は D より先
        Assert.True(bIndex < dIndex, "B should be before D");
        Assert.True(cIndex < dIndex, "C should be before D");
    }

    /// <summary>
    /// ExecutionHistory クラスを使用した統合テスト
    /// EditorGraph を介して履歴が正しく記録されることを確認
    /// </summary>
    [Fact]
    public async Task ExecutionHistory_RecordsNodesInCorrectOrder()
    {
        // Arrange
        var graph = new Graph();

        var nodeA = graph.CreateNode<ConstantNode>();
        nodeA.SetValue(42);

        var nodeB = graph.CreateNode<AddNode>();
        nodeB.ConnectInput(0, nodeA, 0);
        nodeB.ConnectInput(1, nodeA, 0);

        var nodeC = graph.CreateNode<ResultNode>();
        nodeC.ConnectInput(0, nodeB, 0);

        var selectionManager = new SelectionManager();
        var editorGraph = new EditorGraph(graph, selectionManager);

        // ExecutionHistory を作成
        var history = ExecutionHistory.Create(editorGraph);

        // 実行して履歴を記録
        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(
            onExecute: null,
            onExecuted: node => history.Add(node)
        );

        // Assert
        _output.WriteLine($"Total histories: {history.Histories.Count}");
        for (var i = 0; i < history.Histories.Count; i++)
        {
            var h = history.Histories[i];
            var node = graph.Nodes.First(n => n.Id == h.NodeId);
            _output.WriteLine($"  {i + 1}. {node.GetType().Name} (Id: {h.NodeId})");
            _output.WriteLine($"     Inputs: [{string.Join(", ", h.InputValues.Select(v => v?.ToString() ?? "null"))}]");
            _output.WriteLine($"     Outputs: [{string.Join(", ", h.OutputValues.Select(v => v?.ToString() ?? "null"))}]");
        }

        Assert.Equal(3, history.Histories.Count);

        // トポロジカル順序を確認
        var aHistoryIndex = history.Histories.FindIndex(h => h.NodeId == nodeA.Id);
        var bHistoryIndex = history.Histories.FindIndex(h => h.NodeId == nodeB.Id);
        var cHistoryIndex = history.Histories.FindIndex(h => h.NodeId == nodeC.Id);

        Assert.True(aHistoryIndex < bHistoryIndex, "A should be recorded before B in history");
        Assert.True(bHistoryIndex < cHistoryIndex, "B should be recorded before C in history");

        // 値も確認
        var bHistory = history.Histories[bHistoryIndex];
        Assert.Equal(84, bHistory.OutputValues[0]); // 42 + 42 = 84
    }

    /// <summary>
    /// 並列実行されるノードの順序が非決定的であることを確認
    /// 複数回実行して、順序が変わる可能性があることを示す
    /// </summary>
    [Fact]
    public async Task ParallelNodes_OrderMayVary()
    {
        // 複数の独立ノードを作成
        var observedOrders = new List<string>();

        for (var trial = 0; trial < 10; trial++)
        {
            var graph = new Graph();

            // 5つの独立したノード
            var nodes = new List<ConstantNode>();
            for (var i = 0; i < 5; i++)
            {
                var node = graph.CreateNode<ConstantNode>();
                node.SetValue(i);
                nodes.Add(node);
            }

            var executionOrder = new List<NodeId>();
            var executor = graph.CreateExecutor();
            await executor.ExecuteAsync(
                onExecute: null,
                onExecuted: node => executionOrder.Add(node.Id)
            );

            var orderString = string.Join(",", executionOrder.Select(id => nodes.FindIndex(n => n.Id == id)));
            observedOrders.Add(orderString);
        }

        _output.WriteLine("Observed orders:");
        foreach (var order in observedOrders)
        {
            _output.WriteLine($"  {order}");
        }

        // 全ての実行で5ノードが記録される
        Assert.All(observedOrders, order => Assert.Equal(5, order.Split(',').Length));

        // 注意: 並列実行のため、順序が常に同じとは限らない
        // このテストは順序が変わる可能性があることを示すが、
        // 環境によっては常に同じ順序になることもある
    }
}
