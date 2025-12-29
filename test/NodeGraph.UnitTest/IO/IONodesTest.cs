using NodeGraph.Model;

namespace NodeGraph.UnitTest.IO;

/// <summary>
/// I/Oノードのテスト
/// </summary>
public class IONodesTest : IDisposable
{
    private readonly string _testDirectory;

    public IONodesTest()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"NodeGraph_IOTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    #region Path Operations

    [Fact]
    public async Task PathCombineNode_CombinesTwoPaths()
    {
        var graph = new Graph();
        var path1 = graph.CreateNode<StringConstantNode>();
        path1.SetValue("C:\\Users");
        var path2 = graph.CreateNode<StringConstantNode>();
        path2.SetValue("test.txt");

        var combine = graph.CreateNode<PathCombineNode>();
        combine.ConnectInput(0, path1, 0);
        combine.ConnectInput(1, path2, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(combine.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<string>)combine.OutputPorts[0]).Value;
        Assert.Equal(Path.Combine("C:\\Users", "test.txt"), result);
    }

    [Fact]
    public async Task GetFileNameNode_ExtractsFileName()
    {
        var graph = new Graph();
        var path = graph.CreateNode<StringConstantNode>();
        path.SetValue("C:\\Users\\test\\document.txt");

        var getFileName = graph.CreateNode<GetFileNameNode>();
        getFileName.ConnectInput(0, path, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(getFileName.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<string>)getFileName.OutputPorts[0]).Value;
        Assert.Equal("document.txt", result);
    }

    [Fact]
    public async Task GetDirectoryNameNode_ExtractsDirectoryName()
    {
        var graph = new Graph();
        var path = graph.CreateNode<StringConstantNode>();
        path.SetValue("C:\\Users\\test\\document.txt");

        var getDirectoryName = graph.CreateNode<GetDirectoryNameNode>();
        getDirectoryName.ConnectInput(0, path, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(getDirectoryName.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<string>)getDirectoryName.OutputPorts[0]).Value;
        Assert.Equal("C:\\Users\\test", result);
    }

    [Fact]
    public async Task GetExtensionNode_ExtractsExtension()
    {
        var graph = new Graph();
        var path = graph.CreateNode<StringConstantNode>();
        path.SetValue("document.txt");

        var getExtension = graph.CreateNode<GetExtensionNode>();
        getExtension.ConnectInput(0, path, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(getExtension.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<string>)getExtension.OutputPorts[0]).Value;
        Assert.Equal(".txt", result);
    }

    #endregion

    #region File Existence

    [Fact]
    public async Task FileExistsNode_ReturnsTrueForExistingFile()
    {
        var testFile = Path.Combine(_testDirectory, "exists.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var graph = new Graph();
        var path = graph.CreateNode<StringConstantNode>();
        path.SetValue(testFile);

        var fileExists = graph.CreateNode<FileExistsNode>();
        fileExists.ConnectInput(0, path, 0);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<bool>)fileExists.OutputPorts[0]).Value;
        Assert.True(result);
    }

    [Fact]
    public async Task FileExistsNode_ReturnsFalseForNonExistingFile()
    {
        var testFile = Path.Combine(_testDirectory, "nonexistent.txt");

        var graph = new Graph();
        var path = graph.CreateNode<StringConstantNode>();
        path.SetValue(testFile);

        var fileExists = graph.CreateNode<FileExistsNode>();
        fileExists.ConnectInput(0, path, 0);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<bool>)fileExists.OutputPorts[0]).Value;
        Assert.False(result);
    }

    [Fact]
    public async Task DirectoryExistsNode_ReturnsTrueForExistingDirectory()
    {
        var graph = new Graph();
        var path = graph.CreateNode<StringConstantNode>();
        path.SetValue(_testDirectory);

        var dirExists = graph.CreateNode<DirectoryExistsNode>();
        dirExists.ConnectInput(0, path, 0);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<bool>)dirExists.OutputPorts[0]).Value;
        Assert.True(result);
    }

    #endregion

    #region File Read/Write

    [Fact]
    public async Task FileWriteAndReadText_RoundTrip()
    {
        var testFile = Path.Combine(_testDirectory, "roundtrip.txt");
        const string content = "Hello, NodeGraph!";

        // Write
        var writeGraph = new Graph();
        var writePath = writeGraph.CreateNode<StringConstantNode>();
        writePath.SetValue(testFile);
        var writeContent = writeGraph.CreateNode<StringConstantNode>();
        writeContent.SetValue(content);

        var writeNode = writeGraph.CreateNode<FileWriteTextNode>();
        writeNode.ConnectInput(0, writePath, 0);
        writeNode.ConnectInput(1, writeContent, 0);

        var writeStart = writeGraph.CreateNode<StartNode>();
        writeStart.ExecOutPorts[0].Connect(writeNode.ExecInPorts[0]);

        await writeGraph.CreateExecutor().ExecuteAsync();

        // Read
        var readGraph = new Graph();
        var readPath = readGraph.CreateNode<StringConstantNode>();
        readPath.SetValue(testFile);

        var readNode = readGraph.CreateNode<FileReadTextNode>();
        readNode.ConnectInput(0, readPath, 0);

        var readStart = readGraph.CreateNode<StartNode>();
        readStart.ExecOutPorts[0].Connect(readNode.ExecInPorts[0]);

        await readGraph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<string>)readNode.OutputPorts[0]).Value;
        Assert.Equal(content, result);
    }

    [Fact]
    public async Task FileAppendTextNode_AppendsContent()
    {
        var testFile = Path.Combine(_testDirectory, "append.txt");
        await File.WriteAllTextAsync(testFile, "Line1\n");

        var graph = new Graph();
        var path = graph.CreateNode<StringConstantNode>();
        path.SetValue(testFile);
        var content = graph.CreateNode<StringConstantNode>();
        content.SetValue("Line2\n");

        var appendNode = graph.CreateNode<FileAppendTextNode>();
        appendNode.ConnectInput(0, path, 0);
        appendNode.ConnectInput(1, content, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(appendNode.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        var result = await File.ReadAllTextAsync(testFile);
        Assert.Equal("Line1\nLine2\n", result);
    }

    #endregion

    #region Directory Operations

    [Fact]
    public async Task CreateDirectoryNode_CreatesDirectory()
    {
        var newDir = Path.Combine(_testDirectory, "newdir");

        var graph = new Graph();
        var path = graph.CreateNode<StringConstantNode>();
        path.SetValue(newDir);

        var createDir = graph.CreateNode<CreateDirectoryNode>();
        createDir.ConnectInput(0, path, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(createDir.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        Assert.True(Directory.Exists(newDir));
    }

    #endregion

    #region JSON Operations

    [Fact]
    public async Task JsonParseNode_ExtractsSimpleValue()
    {
        const string json = """{"name":"John","age":30}""";

        var graph = new Graph();
        var jsonInput = graph.CreateNode<StringConstantNode>();
        jsonInput.SetValue(json);
        var pathInput = graph.CreateNode<StringConstantNode>();
        pathInput.SetValue("name");

        var parseNode = graph.CreateNode<JsonParseNode>();
        parseNode.ConnectInput(0, jsonInput, 0);
        parseNode.ConnectInput(1, pathInput, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(parseNode.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<string>)parseNode.OutputPorts[0]).Value;
        Assert.Equal("John", result);
    }

    [Fact]
    public async Task JsonParseNode_ExtractsNestedValue()
    {
        const string json = """{"data":{"items":[{"id":1},{"id":2}]}}""";

        var graph = new Graph();
        var jsonInput = graph.CreateNode<StringConstantNode>();
        jsonInput.SetValue(json);
        var pathInput = graph.CreateNode<StringConstantNode>();
        pathInput.SetValue("data.items[1].id");

        var parseNode = graph.CreateNode<JsonParseNode>();
        parseNode.ConnectInput(0, jsonInput, 0);
        parseNode.ConnectInput(1, pathInput, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(parseNode.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<string>)parseNode.OutputPorts[0]).Value;
        Assert.Equal("2", result);
    }

    [Fact]
    public async Task JsonParseNode_ReturnsEmptyForInvalidPath()
    {
        const string json = """{"name":"John"}""";

        var graph = new Graph();
        var jsonInput = graph.CreateNode<StringConstantNode>();
        jsonInput.SetValue(json);
        var pathInput = graph.CreateNode<StringConstantNode>();
        pathInput.SetValue("nonexistent");

        var parseNode = graph.CreateNode<JsonParseNode>();
        parseNode.ConnectInput(0, jsonInput, 0);
        parseNode.ConnectInput(1, pathInput, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(parseNode.ExecInPorts[0]);

        await graph.CreateExecutor().ExecuteAsync();

        var result = ((OutputPort<string>)parseNode.OutputPorts[0]).Value;
        Assert.Equal(string.Empty, result);
    }

    #endregion
}
