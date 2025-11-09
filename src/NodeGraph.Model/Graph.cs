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
        foreach (var port in node.InputPorts)
        {
            port.DisconnectAll();
        }

        foreach (var port in node.OutputPorts)
        {
            port.DisconnectAll();
        }

        Nodes.Remove(node);
    }

    public T[] GetNodes<T>() where T : Node
    {
        using var resultRental = ListPool<T>.Shared.Rent(out var result);
        for (var i = 0; i < Nodes.Count; i++)
        {
            if (Nodes[i] is T node)
            {
                result.Add(node);
            }
        }

        return result.ToArray();
    }

    public GraphExecutor CreateExecutor()
    {
        return new GraphExecutor(this);
    }

    /// <summary>
    /// 完全なクローンの作成
    /// </summary>
    public Graph Clone() => Clone(Nodes.ToArray());

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
            if (Activator.CreateInstance(nodeType) is not Node clonedNode)
            {
                throw new InvalidOperationException($"Failed to create instance of {nodeType.Name}");
            }

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
}