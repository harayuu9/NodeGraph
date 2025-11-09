using System;
using System.Collections.Generic;
using System.Linq;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Undo;

namespace NodeGraph.Editor.Services;

/// <summary>
/// トポロジカルソートに基づくグラフレイアウトサービス
/// ノードを依存関係に基づいて左から右に整列します
/// </summary>
public class TopologicalLayoutService : IGraphLayoutService
{
    /// <summary>
    /// ノードを階層に分類します（トポロジカルソートベース）
    /// </summary>
    public List<List<EditorNode>> ComputeLayers(IEnumerable<EditorNode> nodes, EditorGraph graph)
    {
        var nodeList = nodes.ToList();
        var layers = new List<List<EditorNode>>();
        var nodeToLayer = new Dictionary<EditorNode, int>();

        // 各ノードの入力依存関係を計算
        var dependencies = new Dictionary<EditorNode, List<EditorNode>>();
        foreach (var node in nodeList)
        {
            dependencies[node] = [];
        }

        // 選択されたノード間の接続のみを考慮
        foreach (var connection in graph.Connections)
        {
            if (nodeList.Contains(connection.SourceNode) && nodeList.Contains(connection.TargetNode))
            {
                dependencies[connection.TargetNode].Add(connection.SourceNode);
            }
        }

        // 深さ優先探索で各ノードの階層を決定
        void AssignLayer(EditorNode node, int layer)
        {
            if (nodeToLayer.TryGetValue(node, out var existingLayer))
            {
                // 既に割り当てられている場合は、より深い階層を優先
                if (layer > existingLayer)
                {
                    nodeToLayer[node] = layer;
                }
                return;
            }

            nodeToLayer[node] = layer;

            // このノードに依存しているノードは次の階層
            foreach (var connection in graph.Connections)
            {
                if (connection.SourceNode == node && nodeList.Contains(connection.TargetNode))
                {
                    AssignLayer(connection.TargetNode, layer + 1);
                }
            }
        }

        // 入力依存関係がないノード（ルートノード）から開始
        var rootNodes = nodeList.Where(n => dependencies[n].Count == 0).ToList();

        // ルートノードがない場合（循環依存がある場合）は全ノードをルートとして扱う
        if (rootNodes.Count == 0)
        {
            rootNodes = nodeList.ToList();
        }

        foreach (var rootNode in rootNodes)
        {
            AssignLayer(rootNode, 0);
        }

        // 階層ごとにノードをグループ化
        var maxLayer = nodeToLayer.Values.DefaultIfEmpty(-1).Max();
        for (var i = 0; i <= maxLayer; i++)
        {
            layers.Add([]);
        }

        foreach (var (node, layer) in nodeToLayer)
        {
            layers[layer].Add(node);
        }

        // 階層に属していないノード（孤立ノード）を最初の階層に追加
        var unassignedNodes = nodeList.Where(n => !nodeToLayer.ContainsKey(n)).ToList();
        if (unassignedNodes.Count > 0)
        {
            if (layers.Count == 0)
            {
                layers.Add([]);
            }
            layers[0].AddRange(unassignedNodes);
        }

        return layers;
    }

    /// <summary>
    /// ノードを自動整列し、Undo/Redo可能なアクションを作成します
    /// </summary>
    public ArrangeNodesAction CreateArrangeAction(
        IEnumerable<EditorNode> nodes,
        EditorGraph graph,
        Func<EditorNode, (double width, double height)> getNodeSize)
    {
        var selectedNodes = nodes.ToList();
        if (selectedNodes.Count == 0)
        {
            // 空のアクションを返す（何もしない）
            return new ArrangeNodesAction([]);
        }

        // 古い位置を保存
        var oldPositions = selectedNodes.Select(n => (n, n.X, n.Y)).ToList();

        // ノードを階層ごとに分類（トポロジカルソート）
        var layers = ComputeLayers(selectedNodes, graph);

        // 各階層の配置パラメータ
        const double horizontalSpacing = 100.0; // 階層間の最小水平間隔
        const double verticalSpacing = 20.0;    // 同一階層内のノード間の最小垂直間隔

        // 最初のノードの位置を基準点とする
        var baseX = selectedNodes.Min(n => n.X);
        var baseY = selectedNodes.Min(n => n.Y);

        // ノードサイズのキャッシュを作成
        var nodeSizes = new Dictionary<EditorNode, (double width, double height)>();
        foreach (var node in selectedNodes)
        {
            nodeSizes[node] = getNodeSize(node);
        }

        // パス1: X座標を設定
        var currentX = baseX;

        foreach (var layer in layers)
        {
            var maxWidthInLayer = 0.0;
            foreach (var node in layer)
            {
                node.X = currentX;
                maxWidthInLayer = Math.Max(maxWidthInLayer, nodeSizes[node].width);
            }

            currentX += maxWidthInLayer + horizontalSpacing;
        }

        // パス2: Y座標を設定（前方から順番に確定していく）
        var nodesWithConnections = new HashSet<EditorNode>();
        var nodesWithoutConnections = new List<EditorNode>();

        foreach (var layer in layers)
        {
            foreach (var node in layer)
            {
                var nodeHeight = nodeSizes[node].height;

                // このノードに接続されている前の階層（すでに確定済み）のノードを取得
                var connectedPreviousNodes = graph.Connections
                    .Where(c => c.TargetNode == node && selectedNodes.Contains(c.SourceNode))
                    .Select(c => c.SourceNode)
                    .Distinct()
                    .ToList();

                // このノードから接続されている次の階層のノードを取得
                var connectedNextNodes = graph.Connections
                    .Where(c => c.SourceNode == node && selectedNodes.Contains(c.TargetNode))
                    .Select(c => c.TargetNode)
                    .Distinct()
                    .ToList();

                var hasConnections = connectedPreviousNodes.Count > 0 || connectedNextNodes.Count > 0;

                if (hasConnections)
                {
                    // 接続があるノード
                    nodesWithConnections.Add(node);

                    // 前方のノード（確定済み）の中心Y座標の平均を計算
                    if (connectedPreviousNodes.Count > 0)
                    {
                        var avgCenterY = connectedPreviousNodes.Average(n => n.Y + nodeSizes[n].height / 2.0);
                        node.Y = avgCenterY - nodeHeight / 2.0;
                    }
                    else
                    {
                        // 前方に接続がなく後方にのみ接続がある場合は基準Y座標
                        node.Y = baseY;
                    }
                }
                else
                {
                    // 接続がないノードは後で配置
                    nodesWithoutConnections.Add(node);
                }
            }

            // 同じ階層内で接続があるノードが重ならないように調整
            var sortedLayer = layer.Where(n => nodesWithConnections.Contains(n)).OrderBy(n => n.Y).ToList();
            for (var i = 1; i < sortedLayer.Count; i++)
            {
                var prevNode = sortedLayer[i - 1];
                var currNode = sortedLayer[i];
                var prevNodeBottom = prevNode.Y + nodeSizes[prevNode].height;
                var minY = prevNodeBottom + verticalSpacing;

                if (currNode.Y < minY)
                {
                    currNode.Y = minY;
                }
            }
        }

        // 接続がないノードを階層ごとに配置
        if (nodesWithoutConnections.Count > 0)
        {
            foreach (var layer in layers)
            {
                var disconnectedNodesInLayer = layer.Where(n => nodesWithoutConnections.Contains(n)).ToList();

                if (disconnectedNodesInLayer.Count == 0)
                    continue;

                // この階層の接続があるノードの最大Y座標を取得
                var connectedNodesInLayer = layer.Where(n => nodesWithConnections.Contains(n)).ToList();
                var currentY = connectedNodesInLayer.Count > 0
                    ? connectedNodesInLayer.Max(n => n.Y + nodeSizes[n].height) + verticalSpacing * 2
                    : baseY;

                // 接続がないノードを配置
                foreach (var node in disconnectedNodesInLayer)
                {
                    node.Y = currentY;
                    currentY += nodeSizes[node].height + verticalSpacing;
                }
            }
        }

        // 新しい位置と古い位置を組み合わせてアクションを作成
        var nodePositions = oldPositions
            .Select(old => (old.n, old.X, old.Y, old.n.X, old.n.Y))
            .ToList();

        var action = new ArrangeNodesAction(nodePositions);

        // 一度Undoして、Actionで再実行できるようにする
        foreach (var (node, oldX, oldY, _, _) in nodePositions)
        {
            node.X = oldX;
            node.Y = oldY;
        }

        return action;
    }
}
