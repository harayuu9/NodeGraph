using NodeGraph.Editor.Models;
using NodeGraph.Model;

namespace NodeGraph.Editor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    public EditorGraph TestGraph { get; }

    public MainWindowViewModel()
    {
        // テスト用のグラフを作成
        var graph = new Graph();

        // テスト用のノードを作成
        graph.CreateNode<TestNode1>();
        graph.CreateNode<TestNode2>();
        graph.CreateNode<TestNode3>();
        graph.CreateNode<TestNode4>();

        // EditorGraphでラップ
        TestGraph = new EditorGraph(graph);

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

// テスト用のノードクラス
public class TestNode1 : Node
{
    protected override void InitializePorts() { }
    protected override void BeforeExecute() { }
    protected override void AfterExecute() { }
    protected override System.Threading.Tasks.Task ExecuteCoreAsync(System.Threading.CancellationToken cancellationToken)
    {
        return System.Threading.Tasks.Task.CompletedTask;
    }
}

public class TestNode2 : Node
{
    protected override void InitializePorts() { }
    protected override void BeforeExecute() { }
    protected override void AfterExecute() { }
    protected override System.Threading.Tasks.Task ExecuteCoreAsync(System.Threading.CancellationToken cancellationToken)
    {
        return System.Threading.Tasks.Task.CompletedTask;
    }
}

public class TestNode3 : Node
{
    protected override void InitializePorts() { }
    protected override void BeforeExecute() { }
    protected override void AfterExecute() { }
    protected override System.Threading.Tasks.Task ExecuteCoreAsync(System.Threading.CancellationToken cancellationToken)
    {
        return System.Threading.Tasks.Task.CompletedTask;
    }
}

public class TestNode4 : Node
{
    protected override void InitializePorts() { }
    protected override void BeforeExecute() { }
    protected override void AfterExecute() { }
    protected override System.Threading.Tasks.Task ExecuteCoreAsync(System.Threading.CancellationToken cancellationToken)
    {
        return System.Threading.Tasks.Task.CompletedTask;
    }
}