using Microsoft.Extensions.AI;
using NodeGraph.Model;
using NodeGraph.Model.AI;

namespace NodeGraph.UnitTest.AI;

public class ChatHistoryNodesTest
{
    [Fact]
    public async Task EmptyChatHistoryNode_Should_Create_Empty_List()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var output = emptyNode.OutputPorts[0].ValueObject as IList<ChatMessage>;
        Assert.NotNull(output);
        Assert.Empty(output);
    }

    [Fact]
    public async Task AddSystemMessageNode_Should_Add_Message_To_Empty_History()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var addSystemNode = graph.CreateNode<AddSystemMessageNode>();
        var contentNode = graph.CreateNode<StringConstantNode>();
        contentNode.SetValue("You are a helpful assistant.");

        // Connect: EmptyChatHistory -> AddSystemMessage
        addSystemNode.ConnectInput(0, emptyNode, 0); // history
        addSystemNode.ConnectInput(1, contentNode, 0); // content

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var output = addSystemNode.OutputPorts[0].ValueObject as IList<ChatMessage>;
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(ChatRole.System, output[0].Role);
        Assert.Equal("You are a helpful assistant.", output[0].Text);
    }

    [Fact]
    public async Task AddUserMessageNode_Should_Add_User_Message()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var addUserNode = graph.CreateNode<AddUserMessageNode>();
        var contentNode = graph.CreateNode<StringConstantNode>();
        contentNode.SetValue("Hello!");

        addUserNode.ConnectInput(0, emptyNode, 0);
        addUserNode.ConnectInput(1, contentNode, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var output = addUserNode.OutputPorts[0].ValueObject as IList<ChatMessage>;
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(ChatRole.User, output[0].Role);
        Assert.Equal("Hello!", output[0].Text);
    }

    [Fact]
    public async Task AddAssistantMessageNode_Should_Add_Assistant_Message()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var addAssistantNode = graph.CreateNode<AddAssistantMessageNode>();
        var contentNode = graph.CreateNode<StringConstantNode>();
        contentNode.SetValue("Hi there!");

        addAssistantNode.ConnectInput(0, emptyNode, 0);
        addAssistantNode.ConnectInput(1, contentNode, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var output = addAssistantNode.OutputPorts[0].ValueObject as IList<ChatMessage>;
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(ChatRole.Assistant, output[0].Role);
        Assert.Equal("Hi there!", output[0].Text);
    }

    [Fact]
    public async Task Chain_Should_Build_Multi_Turn_Conversation()
    {
        var graph = new Graph();

        // Create chain: Empty -> AddSystem -> AddUser -> AddAssistant -> AddUser
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var systemNode = graph.CreateNode<AddSystemMessageNode>();
        var user1Node = graph.CreateNode<AddUserMessageNode>();
        var assistantNode = graph.CreateNode<AddAssistantMessageNode>();
        var user2Node = graph.CreateNode<AddUserMessageNode>();

        // String constants for content
        var systemContent = graph.CreateNode<StringConstantNode>();
        systemContent.SetValue("You are helpful.");
        var user1Content = graph.CreateNode<StringConstantNode>();
        user1Content.SetValue("Hello!");
        var assistantContent = graph.CreateNode<StringConstantNode>();
        assistantContent.SetValue("Hi there!");
        var user2Content = graph.CreateNode<StringConstantNode>();
        user2Content.SetValue("How are you?");

        // Connect history chain
        systemNode.ConnectInput(0, emptyNode, 0);
        systemNode.ConnectInput(1, systemContent, 0);

        user1Node.ConnectInput(0, systemNode, 0);
        user1Node.ConnectInput(1, user1Content, 0);

        assistantNode.ConnectInput(0, user1Node, 0);
        assistantNode.ConnectInput(1, assistantContent, 0);

        user2Node.ConnectInput(0, assistantNode, 0);
        user2Node.ConnectInput(1, user2Content, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var output = user2Node.OutputPorts[0].ValueObject as IList<ChatMessage>;
        Assert.NotNull(output);
        Assert.Equal(4, output.Count);
        Assert.Equal(ChatRole.System, output[0].Role);
        Assert.Equal(ChatRole.User, output[1].Role);
        Assert.Equal(ChatRole.Assistant, output[2].Role);
        Assert.Equal(ChatRole.User, output[3].Role);
    }

    [Fact]
    public async Task ChatHistoryLengthNode_Should_Return_Count()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var addNode = graph.CreateNode<AddUserMessageNode>();
        var lengthNode = graph.CreateNode<ChatHistoryLengthNode>();
        var content = graph.CreateNode<StringConstantNode>();
        content.SetValue("Test");

        addNode.ConnectInput(0, emptyNode, 0);
        addNode.ConnectInput(1, content, 0);
        lengthNode.ConnectInput(0, addNode, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var count = (int)lengthNode.OutputPorts[0].ValueObject!;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ChatHistoryLengthNode_Should_Return_Zero_For_Empty_History()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var lengthNode = graph.CreateNode<ChatHistoryLengthNode>();
        lengthNode.ConnectInput(0, emptyNode, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var count = (int)lengthNode.OutputPorts[0].ValueObject!;
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetLastResponseNode_Should_Find_Assistant_Message()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var userNode = graph.CreateNode<AddUserMessageNode>();
        var assistantNode = graph.CreateNode<AddAssistantMessageNode>();
        var getLastNode = graph.CreateNode<GetLastResponseNode>();

        var userContent = graph.CreateNode<StringConstantNode>();
        userContent.SetValue("Hello");
        var assistantContent = graph.CreateNode<StringConstantNode>();
        assistantContent.SetValue("Hi there!");

        userNode.ConnectInput(0, emptyNode, 0);
        userNode.ConnectInput(1, userContent, 0);
        assistantNode.ConnectInput(0, userNode, 0);
        assistantNode.ConnectInput(1, assistantContent, 0);
        getLastNode.ConnectInput(0, assistantNode, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var response = (string)getLastNode.OutputPorts[0].ValueObject!;
        var found = (bool)getLastNode.OutputPorts[1].ValueObject!;

        Assert.True(found);
        Assert.Equal("Hi there!", response);
    }

    [Fact]
    public async Task GetLastResponseNode_Should_Return_False_When_No_Assistant_Message()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var userNode = graph.CreateNode<AddUserMessageNode>();
        var getLastNode = graph.CreateNode<GetLastResponseNode>();

        var userContent = graph.CreateNode<StringConstantNode>();
        userContent.SetValue("Hello");

        userNode.ConnectInput(0, emptyNode, 0);
        userNode.ConnectInput(1, userContent, 0);
        getLastNode.ConnectInput(0, userNode, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var response = (string)getLastNode.OutputPorts[0].ValueObject!;
        var found = (bool)getLastNode.OutputPorts[1].ValueObject!;

        Assert.False(found);
        Assert.Equal(string.Empty, response);
    }

    [Fact]
    public async Task AddSystemMessageNode_With_Default_History_Should_Create_New_List()
    {
        var graph = new Graph();
        var addSystemNode = graph.CreateNode<AddSystemMessageNode>();
        var contentNode = graph.CreateNode<StringConstantNode>();
        contentNode.SetValue("System message");

        // Connect only content (history uses default empty list)
        addSystemNode.ConnectInput(1, contentNode, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var output = addSystemNode.OutputPorts[0].ValueObject as IList<ChatMessage>;
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(ChatRole.System, output[0].Role);
    }

    [Fact]
    public async Task AddMessage_With_Empty_Content_Should_Not_Add_Message()
    {
        var graph = new Graph();
        var emptyNode = graph.CreateNode<EmptyChatHistoryNode>();
        var addUserNode = graph.CreateNode<AddUserMessageNode>();
        var emptyContent = graph.CreateNode<StringConstantNode>();
        emptyContent.SetValue(string.Empty);

        addUserNode.ConnectInput(0, emptyNode, 0);
        addUserNode.ConnectInput(1, emptyContent, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var output = addUserNode.OutputPorts[0].ValueObject as IList<ChatMessage>;
        Assert.NotNull(output);
        Assert.Empty(output);
    }
}
