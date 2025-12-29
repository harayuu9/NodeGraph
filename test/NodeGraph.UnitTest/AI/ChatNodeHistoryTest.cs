using Microsoft.Extensions.AI;
using NodeGraph.Model;
using NodeGraph.Model.AI;

namespace NodeGraph.UnitTest.AI;

/// <summary>
/// ChatNodeのChatHistory入力機能のテスト
/// </summary>
public class ChatNodeHistoryTest
{
    [Fact]
    public async Task ChatNode_Without_Client_Should_Return_Error()
    {
        var graph = new Graph();

        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var userNode = graph.CreateNode<AddUserMessageNode>();
        var chatNode = graph.CreateNode<ChatNode>();
        var start = graph.CreateNode<StartNode>();

        var userContent = graph.CreateNode<StringConstantNode>();
        userContent.SetValue("Hello!");

        userNode.ConnectInput(0, emptyNode, 0);
        userNode.ConnectInput(1, userContent, 0);
        chatNode.ConnectInput(0, userNode, 0);

        // Exec flow
        start.ExecOutPorts[0].Connect(userNode.ExecInPorts[0]);
        userNode.ExecOutPorts[0].Connect(chatNode.ExecInPorts[0]);

        // Don't register IChatClient
        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var response = (string)chatNode.OutputPorts[0].ValueObject!;
        Assert.Contains("[Error]", response);
    }

    [Fact]
    public async Task ChatNode_With_Empty_History_Should_Return_Empty_Response()
    {
        var graph = new Graph();

        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var chatNode = graph.CreateNode<ChatNode>();
        var start = graph.CreateNode<StartNode>();

        chatNode.ConnectInput(0, emptyNode, 0);

        // Exec flow
        start.ExecOutPorts[0].Connect(chatNode.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // With empty history, should return empty response
        var response = (string)chatNode.OutputPorts[0].ValueObject!;
        Assert.Equal(string.Empty, response);
    }

    [Fact]
    public async Task ChatNode_With_Default_Empty_History_Should_Return_Empty_Response()
    {
        var graph = new Graph();

        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var chatNode = graph.CreateNode<ChatNode>();
        var start = graph.CreateNode<StartNode>();

        // Connect empty history
        chatNode.ConnectInput(0, emptyNode, 0);

        // Exec flow
        start.ExecOutPorts[0].Connect(chatNode.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // With empty history, should return empty response (not error)
        var response = (string)chatNode.OutputPorts[0].ValueObject!;
        Assert.Equal(string.Empty, response);
    }

    [Fact]
    public async Task ChatNode_OutputHistory_Should_Be_Initialized()
    {
        var graph = new Graph();

        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var chatNode = graph.CreateNode<ChatNode>();
        var start = graph.CreateNode<StartNode>();

        chatNode.ConnectInput(0, emptyNode, 0);

        // Exec flow
        start.ExecOutPorts[0].Connect(chatNode.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // Output history should be an empty list (not null)
        var outputHistory = chatNode.OutputPorts[1].ValueObject as IList<ChatMessage>;
        Assert.NotNull(outputHistory);
        Assert.Empty(outputHistory);
    }
}
