using NodeGraph.Model;

namespace NodeGraph.UnitTest;

/// <summary>
/// テスト用のPersonクラス
/// </summary>
[JsonNode(DisplayName = "Person", Directory = "Json/Test")]
public partial class Person
{
    [JsonProperty(Description = "名前")] public string Name { get; set; } = "";

    [JsonProperty(Description = "年齢")] public int Age { get; set; }

    [JsonProperty(Description = "メールアドレス", Required = false)]
    public string? Email { get; set; }
}

public class JsonNodeTest
{
    [Fact]
    public async Task PersonDeserializeNode_ShouldDeserializeJson()
    {
        // Arrange
        var graph = new Graph();
        var startNode = graph.CreateNode<StartNode>();
        var deserializeNode = graph.CreateNode<Person.DeserializeNode>();

        // Connect execution flow
        startNode.ExecOutPorts[0].Connect(deserializeNode.ExecInPorts[0]);

        // Set JSON input via InputPort
        ((InputPort<string>)deserializeNode.InputPorts[0]).Value = """{"name":"John","age":30,"email":"john@example.com"}""";

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // Assert
        var nameOutput = (OutputPort<string>)deserializeNode.OutputPorts[0];
        var ageOutput = (OutputPort<int>)deserializeNode.OutputPorts[1];
        var emailOutput = (OutputPort<string?>)deserializeNode.OutputPorts[2];
        var successOutput = (OutputPort<bool>)deserializeNode.OutputPorts[3];
        var errorOutput = (OutputPort<string>)deserializeNode.OutputPorts[4];

        Assert.Equal("John", nameOutput.Value);
        Assert.Equal(30, ageOutput.Value);
        Assert.Equal("john@example.com", emailOutput.Value);
        Assert.True(successOutput.Value);
        Assert.Equal("", errorOutput.Value);
    }

    [Fact]
    public async Task PersonSerializeNode_ShouldSerializeToJson()
    {
        // Arrange
        var graph = new Graph();
        var startNode = graph.CreateNode<StartNode>();
        var serializeNode = graph.CreateNode<Person.SerializeNode>();

        // Connect execution flow
        startNode.ExecOutPorts[0].Connect(serializeNode.ExecInPorts[0]);

        // Set inputs
        ((InputPort<string>)serializeNode.InputPorts[0]).Value = "Jane";
        ((InputPort<int>)serializeNode.InputPorts[1]).Value = 25;
        ((InputPort<string?>)serializeNode.InputPorts[2]).Value = "jane@example.com";

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // Assert
        var jsonOutput = (OutputPort<string>)serializeNode.OutputPorts[0];
        Assert.Contains("\"name\":\"Jane\"", jsonOutput.Value);
        Assert.Contains("\"age\":25", jsonOutput.Value);
    }

    [Fact]
    public void PersonSchemaNode_ShouldOutputSchema()
    {
        // Arrange
        var graph = new Graph();
        var schemaNode = graph.CreateNode<Person.SchemaNode>();

        // Assert - Schema should be set in constructor
        var schemaOutput = (OutputPort<string>)schemaNode.OutputPorts[0];
        Assert.Contains("\"type\":\"object\"", schemaOutput.Value);
        Assert.Contains("\"properties\"", schemaOutput.Value);
        Assert.Contains("\"name\"", schemaOutput.Value);
        Assert.Contains("\"age\"", schemaOutput.Value);
    }

    [Fact]
    public async Task PersonDeserializeNode_ShouldHandleInvalidJson()
    {
        // Arrange
        var graph = new Graph();
        var startNode = graph.CreateNode<StartNode>();
        var deserializeNode = graph.CreateNode<Person.DeserializeNode>();

        // Connect execution flow
        startNode.ExecOutPorts[0].Connect(deserializeNode.ExecInPorts[0]);

        // Set invalid JSON
        ((InputPort<string>)deserializeNode.InputPorts[0]).Value = "invalid json";

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // Assert
        var successOutput = (OutputPort<bool>)deserializeNode.OutputPorts[3];
        var errorOutput = (OutputPort<string>)deserializeNode.OutputPorts[4];

        Assert.False(successOutput.Value);
        Assert.NotEmpty(errorOutput.Value);
    }

    [Fact]
    public void PersonAiStructuredOutputNode_ShouldHaveCorrectStructure()
    {
        // Arrange
        var graph = new Graph();
        var aiOutputNode = graph.CreateNode<Person.AiStructuredOutputNode>();

        // Assert - Port structure
        Assert.Single(aiOutputNode.InputPorts); // Prompt
        Assert.Equal(5, aiOutputNode.OutputPorts.Length); // Name, Age, Email, Success, Error
        Assert.Single(aiOutputNode.ExecInPorts);
        Assert.Single(aiOutputNode.ExecOutPorts);

        // Assert - Port names
        Assert.Equal("Prompt", aiOutputNode.GetInputPortName(0));
        Assert.Equal("Name", aiOutputNode.GetOutputPortName(0));
        Assert.Equal("Age", aiOutputNode.GetOutputPortName(1));
        Assert.Equal("Email", aiOutputNode.GetOutputPortName(2));
        Assert.Equal("Success", aiOutputNode.GetOutputPortName(3));
        Assert.Equal("Error", aiOutputNode.GetOutputPortName(4));

        // Assert - Display name and directory
        Assert.Equal("Person AI Output", aiOutputNode.GetDisplayName());
        Assert.Equal("Json/Test", aiOutputNode.GetDirectory());

        // Assert - Properties (SystemPrompt)
        var props = aiOutputNode.GetProperties();
        Assert.Single(props);
        Assert.Equal("SystemPrompt", props[0].Name);
        Assert.Equal(typeof(string), props[0].Type);
    }

    [Fact]
    public async Task PersonAiStructuredOutputNode_ShouldFailWhenNoChatClient()
    {
        // Arrange
        var graph = new Graph();
        var startNode = graph.CreateNode<StartNode>();
        var aiOutputNode = graph.CreateNode<Person.AiStructuredOutputNode>();

        // Connect execution flow
        startNode.ExecOutPorts[0].Connect(aiOutputNode.ExecInPorts[0]);

        // Set prompt
        ((InputPort<string>)aiOutputNode.InputPorts[0]).Value = "Generate a person";

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // Assert - Should fail gracefully when no IChatClient is registered
        var successOutput = (OutputPort<bool>)aiOutputNode.OutputPorts[3];
        var errorOutput = (OutputPort<string>)aiOutputNode.OutputPorts[4];

        Assert.False(successOutput.Value);
        Assert.Contains("IChatClient is not registered", errorOutput.Value);
    }
}