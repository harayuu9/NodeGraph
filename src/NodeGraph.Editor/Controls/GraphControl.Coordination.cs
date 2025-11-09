using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlの座標変換・検索ヘルパー機能
/// ノード検索、ポート座標取得、座標変換など
/// </summary>
public partial class GraphControl
{
    /// <summary>
    /// 指定されたEditorNodeに対応するNodeControlを検索します
    /// </summary>
    private NodeControl? FindNodeControl(EditorNode node)
    {
        return _canvas?.Children
            .OfType<NodeControl>()
            .FirstOrDefault(nc => nc.Node == node);
    }

    /// <summary>
    /// 指定されたノードとポートの画面上の座標を取得します
    /// </summary>
    private Point? GetPortPosition(EditorNode node, EditorPort port)
    {
        // ノードに対応するNodeControlを検索
        var nodeControl = FindNodeControl(node);

        if (nodeControl == null)
        {
            return null;
        }

        // PortControlを検索
        var portControl = FindPortControl(nodeControl, port);

        // INFO 本来は_canvas?.Childrenで見てるから、ここでnullな分けないんだけど構文解析がアホだから CS8604のワーニング出す
        if (_canvas == null)
        {
            return null;
        }

        // PortControlが自身で中心座標を解決するAPIを使用
        return portControl?.GetCenterIn(_canvas);
    }

    /// <summary>
    /// NodeControl内から指定されたEditorPortに対応するPortControlを検索します
    /// </summary>
    private static PortControl? FindPortControl(NodeControl nodeControl, EditorPort port)
    {
        // VisualTreeをトラバースしてPortControlを検索
        // ItemsControlで生成されたコントロールはVisualTreeに配置される
        return nodeControl.GetVisualDescendants()
            .OfType<PortControl>()
            .FirstOrDefault(pc => pc.Port == port);
    }

    /// <summary>
    /// ポートの中心座標を取得します
    /// </summary>
    private Point? GetPortCenterPosition(PortControl portControl)
    {
        if (_canvas == null)
            return null;

        return portControl.GetCenterIn(_canvas);
    }

    /// <summary>
    /// EditorNodeからNodeControlを作成します
    /// オーバーライドしてカスタムノードコントロールを作成できます
    /// </summary>
    protected virtual Control CreateNodeControl(EditorNode node)
    {
        return new NodeControl { Node = node };
    }
}
