using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;
using NodeGraph.Model;

namespace NodeGraph.Editor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public EditorGraph TestGraph { get; }

    public MainWindowViewModel(SelectionManager selectionManager)
    {
        // テスト用のグラフを作成
        var graph = new Graph();

        // テスト用のノードを作成
        graph.CreateNode<FloatConstantNode>();
        graph.CreateNode<FloatConstantNode>();
        graph.CreateNode<FloatAddNode>();
        graph.CreateNode<FloatResultNode>();

        // EditorGraphでラップ（SelectionManagerを注入）
        TestGraph = new EditorGraph(graph, selectionManager);

        // ノードの位置とサイズを設定
        TestGraph.Nodes[0].PositionX = 100;
        TestGraph.Nodes[0].PositionY = 100;
        TestGraph.Nodes[0].Width = 150;
        TestGraph.Nodes[0].Height = 80;

        TestGraph.Nodes[1].PositionX = 350;
        TestGraph.Nodes[1].PositionY = 50;
        TestGraph.Nodes[1].Width = 150;
        TestGraph.Nodes[1].Height = 100;

        TestGraph.Nodes[2].PositionX = 350;
        TestGraph.Nodes[2].PositionY = 200;
        TestGraph.Nodes[2].Width = 180;
        TestGraph.Nodes[2].Height = 90;

        TestGraph.Nodes[3].PositionX = 600;
        TestGraph.Nodes[3].PositionY = 120;
        TestGraph.Nodes[3].Width = 150;
        TestGraph.Nodes[3].Height = 100;
    }
}