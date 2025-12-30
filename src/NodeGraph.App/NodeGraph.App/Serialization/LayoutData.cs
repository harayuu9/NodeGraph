using System;
using System.Collections.Generic;

namespace NodeGraph.App.Serialization;

/// <summary>
/// レイアウトファイルのルートデータ構造
/// </summary>
public class LayoutData
{
    public string Version { get; set; } = "1.0.0";
    public Dictionary<Guid, NodePosition> Nodes { get; set; } = new();
}

/// <summary>
/// ノードの位置情報
/// </summary>
public class NodePosition
{
    public double X { get; set; }
    public double Y { get; set; }
}