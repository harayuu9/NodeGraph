using NodeGraph.Model.Pool;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NodeGraph.Model.Serialization;

/// <summary>
/// グラフのシリアライズ/デシリアライズを行うクラス
/// </summary>
public static class GraphSerializer
{
    private const string CurrentVersion = "1.0.0";

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>
    /// グラフをYAMLファイルに保存します
    /// </summary>
    public static void SaveToYaml(Graph graph, string filePath) => File.WriteAllText(filePath, Serialize(graph));

    public static string Serialize(Graph graph)
    {
        var graphData = SerializeGraph(graph);
        var yaml = YamlSerializer.Serialize(graphData);
        return yaml;
    }

    /// <summary>
    /// YAMLファイルからグラフを読み込みます
    /// </summary>
    public static Graph LoadFromYaml(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        return Deserialize(yaml);
    }

    public static Graph Deserialize(string yaml)
    {
        var graphData = YamlDeserializer.Deserialize<GraphData>(yaml);
        ValidateVersion(graphData.Version);
        return DeserializeGraph(graphData);
    }
    
    /// <summary>
    /// グラフをGraphDataに変換します
    /// </summary>
    private static GraphData SerializeGraph(Graph graph)
    {
        var graphData = new GraphData
        {
            Version = CurrentVersion
        };

        // ノードをシリアライズ
        foreach (var node in graph.Nodes)
        {
            var nodeData = SerializeNode(node);
            graphData.Nodes.Add(nodeData);
        }

        // 接続をシリアライズ（入力ポートから出力ポートへの接続を記録）
        foreach (var node in graph.Nodes)
        {
            for (var i = 0; i < node.InputPorts.Length; i++)
            {
                var inputPort = node.InputPorts[i];
                if (inputPort is SingleConnectPort { ConnectedPort: not null } singlePort)
                {
                    graphData.Connections.Add(new ConnectionData
                    {
                        Source = new ConnectionEndpoint
                        {
                            NodeId = singlePort.ConnectedPort.Parent.Id.Value,
                            PortId = singlePort.ConnectedPort.Id.Value
                        },
                        Target = new ConnectionEndpoint
                        {
                            NodeId = node.Id.Value,
                            PortId = inputPort.Id.Value
                        }
                    });
                }
            }
        }

        return graphData;
    }

    /// <summary>
    /// ノードをNodeDataに変換します
    /// </summary>
    private static NodeData SerializeNode(Node node)
    {
        var nodeData = new NodeData
        {
            Id = node.Id.Value,
            Type = node.GetType().FullName ?? node.GetType().Name
        };

        // プロパティ値を保存
        var properties = node.GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.Getter(node);
            nodeData.Properties[prop.Name] = value;
        }

        // ポート情報を保存（IDと型情報）
        for (var i = 0; i < node.InputPorts.Length; i++)
        {
            var port = node.InputPorts[i];
            nodeData.Ports.Add(new PortData
            {
                Id = port.Id.Value,
                Direction = "input",
                TypeName = port.PortType.FullName ?? port.PortType.Name,
                Index = i
            });
        }

        for (var i = 0; i < node.OutputPorts.Length; i++)
        {
            var port = node.OutputPorts[i];
            nodeData.Ports.Add(new PortData
            {
                Id = port.Id.Value,
                Direction = "output",
                TypeName = port.PortType.FullName ?? port.PortType.Name,
                Index = i
            });
        }

        return nodeData;
    }

    /// <summary>
    /// GraphDataからグラフを再構築します
    /// </summary>
    private static Graph DeserializeGraph(GraphData graphData)
    {
        var graph = new Graph();
        using var nodeMapRental = DictionaryPool<Guid, Node>.Shared.Rent(out var nodeMap);

        // Pass 1: すべてのノードを作成
        foreach (var nodeData in graphData.Nodes)
        {
            var node = DeserializeNode(nodeData);
            graph.Nodes.Add(node);
            nodeMap[nodeData.Id] = node;
        }

        // Pass 2: 接続を確立
        foreach (var connection in graphData.Connections)
        {
            if (!nodeMap.TryGetValue(connection.Source.NodeId, out var sourceNode))
            {
                throw new InvalidOperationException(
                    $"Source node {connection.Source.NodeId} not found");
            }

            if (!nodeMap.TryGetValue(connection.Target.NodeId, out var targetNode))
            {
                throw new InvalidOperationException(
                    $"Target node {connection.Target.NodeId} not found");
            }

            // ポートIDから実際のポートを検索
            var sourcePort = FindPort(sourceNode.OutputPorts, connection.Source.PortId);
            var targetPort = FindPort(targetNode.InputPorts, connection.Target.PortId);

            if (sourcePort == null)
            {
                throw new InvalidOperationException(
                    $"Source port {connection.Source.PortId} not found in node {sourceNode.GetType().Name}");
            }

            if (targetPort == null)
            {
                throw new InvalidOperationException(
                    $"Target port {connection.Target.PortId} not found in node {targetNode.GetType().Name}");
            }

            // 接続を確立
            targetPort.Connect(sourcePort);
        }

        return graph;
    }

    /// <summary>
    /// NodeDataからノードを作成します
    /// </summary>
    private static Node DeserializeNode(NodeData nodeData)
    {
        // ノードタイプを検索
        var nodeType = FindNodeType(nodeData.Type);
        if (nodeType == null)
        {
            throw new InvalidOperationException(
                $"Node type '{nodeData.Type}' not found. " +
                $"Make sure the assembly containing this type is loaded.");
        }

        // ノードインスタンスを作成
        var node = CreateNodeWithPorts(nodeType, nodeData);

        // プロパティ値を復元
        var properties = node.GetProperties();
        foreach (var kvp in nodeData.Properties)
        {
            // プロパティの型情報を取得して、値を変換
            var propDescriptor = Array.Find(properties, p => p.Name == kvp.Key);
            if (propDescriptor != null && kvp.Value != null)
            {
                // YAMLデシリアライザは数値を適切な型に変換しないことがあるため、型変換を行う
                var convertedValue = ConvertValue(kvp.Value, propDescriptor.Type);
                node.SetPropertyValue(kvp.Key, convertedValue);
            }
            else
            {
                node.SetPropertyValue(kvp.Key, kvp.Value);
            }
        }

        return node;
    }

    /// <summary>
    /// ノードをポートIDを指定して作成します
    /// </summary>
    private static Node CreateNodeWithPorts(Type nodeType, NodeData nodeData)
    {
        // ポートIDを入力と出力に分ける
        var inputPortIds = nodeData.Ports
            .Where(p => p.Direction == "input")
            .OrderBy(p => p.Index)
            .Select(p => new PortId(p.Id))
            .ToArray();

        var outputPortIds = nodeData.Ports
            .Where(p => p.Direction == "output")
            .OrderBy(p => p.Index)
            .Select(p => new PortId(p.Id))
            .ToArray();

        // デシリアライズ用コンストラクタでノードを作成
        var nodeId = new NodeId(nodeData.Id);
        if (Activator.CreateInstance(nodeType, nodeId, inputPortIds, outputPortIds) is not Node node)
        {
            throw new InvalidOperationException($"Failed to create instance of {nodeType.Name}");
        }

        return node;
    }

    /// <summary>
    /// ポート配列からIDが一致するポートを検索します
    /// </summary>
    private static Port? FindPort<T>(T[] ports, Guid portId)
        where T : Port
    {
        foreach (var port in ports)
        {
            if (port.Id.Value == portId)
            {
                return port;
            }
        }
        return null;
    }

    /// <summary>
    /// 型名からノードタイプを検索します
    /// </summary>
    private static Type? FindNodeType(string typeName)
    {
        // まず現在のアセンブリから検索
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        // すべてのロード済みアセンブリから検索
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>
    /// 値を指定された型に変換します
    /// </summary>
    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return null;
        }

        // 既に正しい型の場合はそのまま返す
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        // 型変換を試みる
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            // 変換に失敗した場合は元の値を返す
            return value;
        }
    }

    /// <summary>
    /// バージョンを検証します
    /// </summary>
    private static void ValidateVersion(string version)
    {
        if (!Version.TryParse(version, out var fileVersion) ||
            !Version.TryParse(CurrentVersion, out var currentVersion))
        {
            throw new InvalidOperationException($"Invalid version format: {version}");
        }

        // メジャーバージョンが異なる場合はエラー
        if (fileVersion.Major != currentVersion.Major)
        {
            throw new InvalidOperationException(
                $"Incompatible file version: {version}. " +
                $"Current version: {CurrentVersion}. " +
                $"Major version mismatch detected. Please use a compatible version or migrate the file.");
        }

        // マイナーバージョンが新しい場合は警告（将来的にはログに出力）
        if (fileVersion.Minor > currentVersion.Minor)
        {
            // TODO: ロギング機能を追加したら警告を出力
            // Console.WriteLine($"Warning: File was created with a newer version ({version})");
        }
    }
}
