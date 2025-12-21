using System.Text;
using NodeGraph.Model.Pool;

namespace NodeGraph.Model;

public class Graph
{
    public List<Node> Nodes { get; } = [];

    public T CreateNode<T>() where T : Node, new()
    {
        var node = new T();
        Nodes.Add(node);
        return node;
    }

    public bool AddNode(Node node)
    {
        if (!Nodes.Contains(node))
        {
            Nodes.Add(node);
            return true;
        }

        return false;
    }

    public void RemoveNode(Node node)
    {
        // ノードの接続を全て解除
        foreach (var port in node.InputPorts) port.DisconnectAll();

        foreach (var port in node.OutputPorts) port.DisconnectAll();

        Nodes.Remove(node);
    }

    public T[] GetNodes<T>() where T : Node
    {
        using var resultRental = ListPool<T>.Shared.Rent(out var result);
        for (var i = 0; i < Nodes.Count; i++)
            if (Nodes[i] is T node)
                result.Add(node);

        return result.ToArray();
    }

    public GraphExecutor CreateExecutor()
    {
        return new GraphExecutor(this);
    }

    /// <summary>
    /// 完全なクローンの作成
    /// </summary>
    public Graph Clone()
    {
        return Clone(Nodes.ToArray());
    }

    /// <summary>
    /// 特定のNodeのみ含めてクローン
    /// 接続状態も含まれているノード内で完結しているものは複製
    /// </summary>
    public static Graph Clone(Node[] nodes)
    {
        var newGraph = new Graph();
        using var _ = DictionaryPool<Node, Node>.Shared.Rent(out var nodeMap);

        // 1. 全ノードをクローン（新しいIDで作成）
        foreach (var node in nodes)
        {
            var nodeType = node.GetType();
            if (Activator.CreateInstance(nodeType) is not Node clonedNode) throw new InvalidOperationException($"Failed to create instance of {nodeType.Name}");

            // プロパティ値をコピー
            var properties = node.GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.Getter(node);
                prop.Setter(clonedNode, value);
            }

            newGraph.AddNode(clonedNode);
            nodeMap[node] = clonedNode;
        }

        // クローン対象のノード内で完結している接続のみ復元
        foreach (var originalNode in nodes)
        {
            var clonedNode = nodeMap[originalNode];

            // 出力ポートの接続を復元
            for (var i = 0; i < originalNode.OutputPorts.Length; i++)
            {
                MultiConnectPort originalOutputPort = originalNode.OutputPorts[i];
                var clonedOutputPort = clonedNode.OutputPorts[i];

                foreach (var port in originalOutputPort.ConnectedPorts)
                {
                    var connectedInputPort = (InputPort)port;
                    var connectedNode = connectedInputPort.Parent;

                    // 接続先のノードがクローン対象に含まれている場合のみ接続
                    if (nodeMap.TryGetValue(connectedNode, out var clonedConnectedNode))
                    {
                        var portIndex = Array.IndexOf(connectedNode.InputPorts, connectedInputPort);
                        if (portIndex >= 0)
                        {
                            var clonedInputPort = clonedConnectedNode.InputPorts[portIndex];
                            clonedInputPort.Connect(clonedOutputPort);
                        }
                    }
                }
            }
        }

        return newGraph;
    }

    /// <summary>
    /// グラフの接続状況をMermaid形式の文字列として出力します。
    /// </summary>
    /// <param name="direction">グラフの方向 (LR: 左から右, TD: 上から下, RL: 右から左, BT: 下から上)</param>
    /// <returns>Mermaid形式のグラフ定義文字列</returns>
    public string ToMermaid(string direction = "LR")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"graph {direction}");

        // ノードIDとMermaid用識別子のマッピングを作成
        using var _ = DictionaryPool<NodeId, string>.Shared.Rent(out var nodeIdMap);
        var nodeIndex = 0;
        foreach (var node in Nodes)
        {
            var mermaidId = $"node_{nodeIndex++}";
            nodeIdMap[node.Id] = mermaidId;

            // ノード定義を出力
            var displayName = node.GetDisplayName();
            sb.AppendLine($"    {mermaidId}[\"{EscapeMermaidString(displayName)}\"]");
        }

        // データポートの接続を出力
        foreach (var node in Nodes)
        {
            var sourceId = nodeIdMap[node.Id];

            for (var outputIndex = 0; outputIndex < node.OutputPorts.Length; outputIndex++)
            {
                var outputPort = node.OutputPorts[outputIndex];
                var outputPortName = node.GetOutputPortName(outputIndex);

                foreach (var connectedPort in outputPort.ConnectedPorts)
                    if (connectedPort is InputPort inputPort)
                    {
                        var targetNode = inputPort.Parent;
                        if (nodeIdMap.TryGetValue(targetNode.Id, out var targetId))
                        {
                            var inputIndex = Array.IndexOf(targetNode.InputPorts, inputPort);
                            var inputPortName = inputIndex >= 0 ? targetNode.GetInputPortName(inputIndex) : "?";

                            sb.AppendLine($"    {sourceId} -->|\"{EscapeMermaidString(outputPortName)} → {EscapeMermaidString(inputPortName)}\"| {targetId}");
                        }
                    }
            }
        }

        // 実行フローポートの接続を出力
        foreach (var node in Nodes)
        {
            var sourceId = nodeIdMap[node.Id];

            for (var execOutIndex = 0; execOutIndex < node.ExecOutPorts.Length; execOutIndex++)
            {
                var execOutPort = node.ExecOutPorts[execOutIndex];
                var execOutPortName = node.GetExecOutPortName(execOutIndex);

                if (execOutPort.ConnectedPort is ExecInPort execInPort)
                {
                    var targetNode = execInPort.Parent;
                    if (nodeIdMap.TryGetValue(targetNode.Id, out var targetId))
                    {
                        var execInIndex = Array.IndexOf(targetNode.ExecInPorts, execInPort);
                        var execInPortName = execInIndex >= 0 ? targetNode.GetExecInPortName(execInIndex) : "?";

                        // 実行フローは破線で表現
                        sb.AppendLine($"    {sourceId} -.->|\"{EscapeMermaidString(execOutPortName)} ⇒ {EscapeMermaidString(execInPortName)}\"| {targetId}");
                    }
                }
            }
        }

        return sb.ToString();
    }

    private static string EscapeMermaidString(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "#quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}