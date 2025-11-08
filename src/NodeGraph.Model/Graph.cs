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

    public void AddNode(Node node)
    {
        if (!Nodes.Contains(node))
        {
            Nodes.Add(node);
        }
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
        var result = new List<T>(Nodes.Count);
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
}