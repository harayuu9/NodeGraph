namespace NodeGraph.Model.Serialization;

/// <summary>
/// グラフファイルのルートデータ構造
/// </summary>
public class GraphData
{
    public string Version { get; set; } = "1.0.0";
    public List<NodeData> Nodes { get; set; } = [];
    public List<ConnectionData> Connections { get; set; } = [];
}

/// <summary>
/// ノードのシリアライズデータ
/// </summary>
public class NodeData
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object?> Properties { get; set; } = new();
    public List<PortData> Ports { get; set; } = [];
}

/// <summary>
/// ポートのシリアライズデータ
/// </summary>
public class PortData
{
    public Guid Id { get; set; }
    public string Direction { get; set; } = string.Empty; // "input" or "output"
    public string TypeName { get; set; } = string.Empty;
    public int Index { get; set; }
}

/// <summary>
/// 接続のシリアライズデータ
/// </summary>
public class ConnectionData
{
    public ConnectionEndpoint Source { get; set; } = new();
    public ConnectionEndpoint Target { get; set; } = new();
}

/// <summary>
/// 接続エンドポイント（ノードIDとポートID）
/// </summary>
public class ConnectionEndpoint
{
    public Guid NodeId { get; set; }
    public Guid PortId { get; set; }
}
