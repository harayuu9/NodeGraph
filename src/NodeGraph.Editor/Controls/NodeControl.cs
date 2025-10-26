using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using NodeGraph.Editor.Models;
using NodeGraph.Model;

namespace NodeGraph.Editor.Controls;

public partial class TestNode : Node
{
    protected override void InitializePorts()
    {
        throw new System.NotImplementedException();
    }

    protected override void BeforeExecute()
    {
        throw new System.NotImplementedException();
    }

    protected override void AfterExecute()
    {
        throw new System.NotImplementedException();
    }

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}

[PseudoClasses(":selected")]
public class NodeControl : ContentControl
{
    public NodeControl()
    {
        Node = new EditorNode(new TestNode())
        {
            Width = 100,
            Height = 100,
        };
    }
    
    public static readonly StyledProperty<EditorNode?> NodeProperty = AvaloniaProperty.Register<NodeControl, EditorNode?>(nameof(Node));
    
    public EditorNode? Node
    {
        get => GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }
}