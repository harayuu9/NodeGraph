using NodeGraph.Model;

namespace NodeGraph.UnitTest;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var graph = new Graph();
        
        var nodeA = new Node();
        nodeA.OutputPorts.Add(new Port(nodeA));
        var nodeAOutputPort = nodeA.OutputPorts[0];
        
        var nodeB = new Node();
        nodeB.InputPorts.Add(new Port(nodeB));
        var nodeBInputPort = nodeB.InputPorts[0];
        nodeBInputPort.ConnectedPort = nodeAOutputPort;
        nodeAOutputPort.ConnectedPort = nodeBInputPort;
        
        graph.Nodes.Add(nodeA);
        graph.Nodes.Add(nodeB);
    }
}