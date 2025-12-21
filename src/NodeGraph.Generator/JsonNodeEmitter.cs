using Microsoft.CodeAnalysis;

namespace NodeGraph.Generator;

/// <summary>
/// JSON関連ノードのコード生成
/// </summary>
public static class JsonNodeEmitter
{
    /// <summary>
    /// Deserializeノードを生成（JSON文字列→各プロパティに分解）
    /// </summary>
    public static void EmitDeserializeNode(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        string ns,
        string displayName,
        string directory,
        List<JsonPropertyInfo> properties,
        string fullTypeName)
    {
        var className = "DeserializeNode";
        var inputCount = 1; // _json
        var outputCount = properties.Count + 2; // properties + _success + _error
        var execInCount = 1;
        var execOutCount = 1;

        var codeGen = new CSharpCodeGenerator(ns);
        codeGen.WriteLine($"partial class {typeSymbol.Name}");
        using (codeGen.Scope())
        {
            codeGen.WriteLine($"public class {className} : global::NodeGraph.Model.Node");

            using (codeGen.Scope())
            {
                // JsonSerializerOptions
                codeGen.WriteLine("private static readonly global::System.Text.Json.JsonSerializerOptions JsonOptions = new global::System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };");
                codeGen.WriteLine();

                // フィールド宣言
                codeGen.WriteLine("private string _json = \"\";");
                foreach (var prop in properties)
                {
                    var defaultValue = GetDefaultValue(prop.TypeSymbol);
                    codeGen.WriteLine($"private {prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} _{ToCamelCase(prop.Name)}{defaultValue};");
                }

                codeGen.WriteLine("private bool _success;");
                codeGen.WriteLine("private string _error = \"\";");
                codeGen.WriteLine();

                // デフォルトコンストラクタ
                codeGen.WriteLine($"public {className}() : base({inputCount}, {outputCount}, {execInCount}, {execOutCount})");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("InputPorts[0] = new global::NodeGraph.Model.InputPort<string>(this, _json);");

                    var outputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"OutputPorts[{outputIndex}] = new global::NodeGraph.Model.OutputPort<{propType}>(this, _{ToCamelCase(prop.Name)});");
                        outputIndex++;
                    }

                    codeGen.WriteLine($"OutputPorts[{outputIndex}] = new global::NodeGraph.Model.OutputPort<bool>(this, _success);");
                    codeGen.WriteLine($"OutputPorts[{outputIndex + 1}] = new global::NodeGraph.Model.OutputPort<string>(this, _error);");

                    codeGen.WriteLine("ExecInPorts[0] = new global::NodeGraph.Model.ExecInPort(this);");
                    codeGen.WriteLine("ExecOutPorts[0] = new global::NodeGraph.Model.ExecOutPort(this);");
                }

                codeGen.WriteLine();

                // デシリアライズ用コンストラクタ
                codeGen.WriteLine($"public {className}(global::NodeGraph.Model.NodeId nodeId, global::NodeGraph.Model.PortId[] inputPortIds, global::NodeGraph.Model.PortId[] outputPortIds, global::NodeGraph.Model.PortId[] execInPortIds, global::NodeGraph.Model.PortId[] execOutPortIds) : base(nodeId, inputPortIds, outputPortIds, execInPortIds, execOutPortIds)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("InputPorts[0] = new global::NodeGraph.Model.InputPort<string>(this, inputPortIds[0], _json);");

                    var outputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"OutputPorts[{outputIndex}] = new global::NodeGraph.Model.OutputPort<{propType}>(this, outputPortIds[{outputIndex}], _{ToCamelCase(prop.Name)});");
                        outputIndex++;
                    }

                    codeGen.WriteLine($"OutputPorts[{outputIndex}] = new global::NodeGraph.Model.OutputPort<bool>(this, outputPortIds[{outputIndex}], _success);");
                    codeGen.WriteLine($"OutputPorts[{outputIndex + 1}] = new global::NodeGraph.Model.OutputPort<string>(this, outputPortIds[{outputIndex + 1}], _error);");

                    codeGen.WriteLine("ExecInPorts[0] = new global::NodeGraph.Model.ExecInPort(this, execInPortIds[0]);");
                    codeGen.WriteLine("ExecOutPorts[0] = new global::NodeGraph.Model.ExecOutPort(this, execOutPortIds[0]);");
                }

                codeGen.WriteLine();

                // BeforeExecute
                codeGen.WriteLine("protected override void BeforeExecute()");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("_json = ((global::NodeGraph.Model.InputPort<string>)InputPorts[0]).Value;");
                }

                codeGen.WriteLine();

                // AfterExecute
                codeGen.WriteLine("protected override void AfterExecute()");
                using (codeGen.Scope())
                {
                    var outputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"((global::NodeGraph.Model.OutputPort<{propType}>)OutputPorts[{outputIndex}]).Value = _{ToCamelCase(prop.Name)};");
                        outputIndex++;
                    }

                    codeGen.WriteLine($"((global::NodeGraph.Model.OutputPort<bool>)OutputPorts[{outputIndex}]).Value = _success;");
                    codeGen.WriteLine($"((global::NodeGraph.Model.OutputPort<string>)OutputPorts[{outputIndex + 1}]).Value = _error;");
                }

                codeGen.WriteLine();

                // ExecuteCoreAsync
                codeGen.WriteLine("protected override async global::System.Threading.Tasks.Task ExecuteCoreAsync(global::NodeGraph.Model.NodeExecutionContext context)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("try");
                    using (codeGen.Scope())
                    {
                        codeGen.WriteLine($"var obj = global::System.Text.Json.JsonSerializer.Deserialize<{fullTypeName}>(_json, JsonOptions);");
                        codeGen.WriteLine("if (obj != null)");
                        using (codeGen.Scope())
                        {
                            foreach (var prop in properties) codeGen.WriteLine($"_{ToCamelCase(prop.Name)} = obj.{prop.Name};");
                            codeGen.WriteLine("_success = true;");
                            codeGen.WriteLine("_error = \"\";");
                        }

                        codeGen.WriteLine("else");
                        using (codeGen.Scope())
                        {
                            codeGen.WriteLine("_success = false;");
                            codeGen.WriteLine("_error = \"Deserialization returned null\";");
                        }
                    }

                    codeGen.WriteLine("catch (global::System.Text.Json.JsonException ex)");
                    using (codeGen.Scope())
                    {
                        codeGen.WriteLine("_success = false;");
                        codeGen.WriteLine("_error = ex.Message;");
                    }

                    codeGen.WriteLine("await context.ExecuteOutAsync(0);");
                }

                codeGen.WriteLine();

                // ポート名取得メソッド
                EmitPortNameMethods(codeGen, properties, true);

                // GetDisplayName
                codeGen.WriteLine($"public override string GetDisplayName() => \"{displayName} Deserialize\";");

                // GetDirectory
                codeGen.WriteLine($"public override string GetDirectory() => \"{directory}\";");
            }
        }

        var fileName = $"{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{typeSymbol.Name}.{className}.JsonNodeGenerator.g.cs";
        context.AddSource(fileName, codeGen.GetResult());
    }

    /// <summary>
    /// Serializeノードを生成（各プロパティ→JSON文字列）
    /// </summary>
    public static void EmitSerializeNode(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        string ns,
        string displayName,
        string directory,
        List<JsonPropertyInfo> properties,
        string fullTypeName)
    {
        var className = "SerializeNode";
        var inputCount = properties.Count;
        var outputCount = 1; // _json
        var execInCount = 1;
        var execOutCount = 1;

        var codeGen = new CSharpCodeGenerator(ns);
        codeGen.WriteLine($"partial class {typeSymbol.Name}");
        using (codeGen.Scope())
        {
            codeGen.WriteLine($"public class {className} : global::NodeGraph.Model.Node");

            using (codeGen.Scope())
            {
                // JsonSerializerOptions
                codeGen.WriteLine("private static readonly global::System.Text.Json.JsonSerializerOptions JsonOptions = new global::System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = global::System.Text.Json.JsonNamingPolicy.CamelCase };");
                codeGen.WriteLine();

                // フィールド宣言
                foreach (var prop in properties)
                {
                    var defaultValue = GetDefaultValue(prop.TypeSymbol);
                    codeGen.WriteLine($"private {prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} _{ToCamelCase(prop.Name)}{defaultValue};");
                }

                codeGen.WriteLine("private string _json = \"\";");
                codeGen.WriteLine();

                // デフォルトコンストラクタ
                codeGen.WriteLine($"public {className}() : base({inputCount}, {outputCount}, {execInCount}, {execOutCount})");
                using (codeGen.Scope())
                {
                    var inputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"InputPorts[{inputIndex}] = new global::NodeGraph.Model.InputPort<{propType}>(this, _{ToCamelCase(prop.Name)});");
                        inputIndex++;
                    }

                    codeGen.WriteLine("OutputPorts[0] = new global::NodeGraph.Model.OutputPort<string>(this, _json);");
                    codeGen.WriteLine("ExecInPorts[0] = new global::NodeGraph.Model.ExecInPort(this);");
                    codeGen.WriteLine("ExecOutPorts[0] = new global::NodeGraph.Model.ExecOutPort(this);");
                }

                codeGen.WriteLine();

                // デシリアライズ用コンストラクタ
                codeGen.WriteLine($"public {className}(global::NodeGraph.Model.NodeId nodeId, global::NodeGraph.Model.PortId[] inputPortIds, global::NodeGraph.Model.PortId[] outputPortIds, global::NodeGraph.Model.PortId[] execInPortIds, global::NodeGraph.Model.PortId[] execOutPortIds) : base(nodeId, inputPortIds, outputPortIds, execInPortIds, execOutPortIds)");
                using (codeGen.Scope())
                {
                    var inputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"InputPorts[{inputIndex}] = new global::NodeGraph.Model.InputPort<{propType}>(this, inputPortIds[{inputIndex}], _{ToCamelCase(prop.Name)});");
                        inputIndex++;
                    }

                    codeGen.WriteLine("OutputPorts[0] = new global::NodeGraph.Model.OutputPort<string>(this, outputPortIds[0], _json);");
                    codeGen.WriteLine("ExecInPorts[0] = new global::NodeGraph.Model.ExecInPort(this, execInPortIds[0]);");
                    codeGen.WriteLine("ExecOutPorts[0] = new global::NodeGraph.Model.ExecOutPort(this, execOutPortIds[0]);");
                }

                codeGen.WriteLine();

                // BeforeExecute
                codeGen.WriteLine("protected override void BeforeExecute()");
                using (codeGen.Scope())
                {
                    var inputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"_{ToCamelCase(prop.Name)} = ((global::NodeGraph.Model.InputPort<{propType}>)InputPorts[{inputIndex}]).Value;");
                        inputIndex++;
                    }
                }

                codeGen.WriteLine();

                // AfterExecute
                codeGen.WriteLine("protected override void AfterExecute()");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("((global::NodeGraph.Model.OutputPort<string>)OutputPorts[0]).Value = _json;");
                }

                codeGen.WriteLine();

                // ExecuteCoreAsync
                codeGen.WriteLine("protected override async global::System.Threading.Tasks.Task ExecuteCoreAsync(global::NodeGraph.Model.NodeExecutionContext context)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine($"var obj = new {fullTypeName}();");
                    foreach (var prop in properties) codeGen.WriteLine($"obj.{prop.Name} = _{ToCamelCase(prop.Name)};");
                    codeGen.WriteLine("_json = global::System.Text.Json.JsonSerializer.Serialize(obj, JsonOptions);");
                    codeGen.WriteLine("await context.ExecuteOutAsync(0);");
                }

                codeGen.WriteLine();

                // ポート名取得メソッド
                EmitPortNameMethods(codeGen, properties, false);

                // GetDisplayName
                codeGen.WriteLine($"public override string GetDisplayName() => \"{displayName} Serialize\";");

                // GetDirectory
                codeGen.WriteLine($"public override string GetDirectory() => \"{directory}\";");
            }
        }

        var fileName = $"{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{typeSymbol.Name}.{className}.JsonNodeGenerator.g.cs";
        context.AddSource(fileName, codeGen.GetResult());
    }

    /// <summary>
    /// Schemaノードを生成（スキーマを文字列で出力）
    /// </summary>
    public static void EmitSchemaNode(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        string ns,
        string displayName,
        string directory,
        string schemaJson)
    {
        var className = "SchemaNode";

        var codeGen = new CSharpCodeGenerator(ns);
        codeGen.WriteLine($"partial class {typeSymbol.Name}");
        using (codeGen.Scope())
        {
            codeGen.WriteLine($"public class {className} : global::NodeGraph.Model.Node");

            using (codeGen.Scope())
            {
                // スキーマ定数
                var escapedSchema = schemaJson.Replace("\"", "\"\"");
                codeGen.WriteLine($"private const string SchemaJson = @\"{escapedSchema}\";");
                codeGen.WriteLine();

                // デフォルトコンストラクタ
                codeGen.WriteLine($"public {className}() : base(0, 1, 0, 0)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("OutputPorts[0] = new global::NodeGraph.Model.OutputPort<string>(this, SchemaJson);");
                }

                codeGen.WriteLine();

                // デシリアライズ用コンストラクタ
                codeGen.WriteLine($"public {className}(global::NodeGraph.Model.NodeId nodeId, global::NodeGraph.Model.PortId[] inputPortIds, global::NodeGraph.Model.PortId[] outputPortIds, global::NodeGraph.Model.PortId[] execInPortIds, global::NodeGraph.Model.PortId[] execOutPortIds) : base(nodeId, inputPortIds, outputPortIds, execInPortIds, execOutPortIds)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("OutputPorts[0] = new global::NodeGraph.Model.OutputPort<string>(this, outputPortIds[0], SchemaJson);");
                }

                codeGen.WriteLine();

                // BeforeExecute
                codeGen.WriteLine("protected override void BeforeExecute() { }");
                codeGen.WriteLine();

                // AfterExecute
                codeGen.WriteLine("protected override void AfterExecute()");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("((global::NodeGraph.Model.OutputPort<string>)OutputPorts[0]).Value = SchemaJson;");
                }

                codeGen.WriteLine();

                // ExecuteCoreAsync
                codeGen.WriteLine("protected override global::System.Threading.Tasks.Task ExecuteCoreAsync(global::NodeGraph.Model.NodeExecutionContext context)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("return global::System.Threading.Tasks.Task.CompletedTask;");
                }

                codeGen.WriteLine();

                // ポート名取得メソッド
                codeGen.WriteLine("public override string GetInputPortName(int index)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("throw new global::System.InvalidOperationException(\"Invalid input port index\");");
                }

                codeGen.WriteLine("public override string GetOutputPortName(int index)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("return index == 0 ? \"Schema\" : throw new global::System.InvalidOperationException(\"Invalid output port index\");");
                }

                codeGen.WriteLine("public override string GetExecInPortName(int index)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("throw new global::System.InvalidOperationException(\"Invalid exec in port index\");");
                }

                codeGen.WriteLine("public override string GetExecOutPortName(int index)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("throw new global::System.InvalidOperationException(\"Invalid exec out port index\");");
                }

                // GetDisplayName
                codeGen.WriteLine($"public override string GetDisplayName() => \"{displayName} Schema\";");

                // GetDirectory
                codeGen.WriteLine($"public override string GetDirectory() => \"{directory}\";");
            }
        }

        var fileName = $"{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{typeSymbol.Name}.{className}.JsonNodeGenerator.g.cs";
        context.AddSource(fileName, codeGen.GetResult());
    }

    private static void EmitPortNameMethods(CSharpCodeGenerator codeGen, List<JsonPropertyInfo> properties, bool isDeserialize)
    {
        // GetInputPortName
        codeGen.WriteLine("public override string GetInputPortName(int index)");
        using (codeGen.Scope())
        {
            if (isDeserialize)
            {
                codeGen.WriteLine("return index == 0 ? \"Json\" : throw new global::System.InvalidOperationException(\"Invalid input port index\");");
            }
            else
            {
                codeGen.WriteLine("switch(index)");
                using (codeGen.Scope())
                {
                    for (var i = 0; i < properties.Count; i++) codeGen.WriteLine($"case {i}: return \"{properties[i].Name}\";");
                    codeGen.WriteLine("default: throw new global::System.InvalidOperationException(\"Invalid input port index\");");
                }
            }
        }

        // GetOutputPortName
        codeGen.WriteLine("public override string GetOutputPortName(int index)");
        using (codeGen.Scope())
        {
            if (isDeserialize)
            {
                codeGen.WriteLine("switch(index)");
                using (codeGen.Scope())
                {
                    for (var i = 0; i < properties.Count; i++) codeGen.WriteLine($"case {i}: return \"{properties[i].Name}\";");
                    codeGen.WriteLine($"case {properties.Count}: return \"Success\";");
                    codeGen.WriteLine($"case {properties.Count + 1}: return \"Error\";");
                    codeGen.WriteLine("default: throw new global::System.InvalidOperationException(\"Invalid output port index\");");
                }
            }
            else
            {
                codeGen.WriteLine("return index == 0 ? \"Json\" : throw new global::System.InvalidOperationException(\"Invalid output port index\");");
            }
        }

        // GetExecInPortName
        codeGen.WriteLine("public override string GetExecInPortName(int index)");
        using (codeGen.Scope())
        {
            codeGen.WriteLine("return index == 0 ? \"ExecIn\" : throw new global::System.InvalidOperationException(\"Invalid exec in port index\");");
        }

        // GetExecOutPortName
        codeGen.WriteLine("public override string GetExecOutPortName(int index)");
        using (codeGen.Scope())
        {
            codeGen.WriteLine("return index == 0 ? \"Out\" : throw new global::System.InvalidOperationException(\"Invalid exec out port index\");");
        }
    }

    /// <summary>
    /// AI構造化出力ノードを生成（プロンプト→AI→各プロパティに分解）
    /// </summary>
    public static void EmitAiStructuredOutputNode(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        string ns,
        string displayName,
        string directory,
        List<JsonPropertyInfo> properties,
        string fullTypeName,
        string schemaJson)
    {
        var className = "AiStructuredOutputNode";
        var inputCount = 1; // _prompt
        var outputCount = properties.Count + 2; // properties + _success + _error
        var execInCount = 1;
        var execOutCount = 1;

        var codeGen = new CSharpCodeGenerator(ns);
        codeGen.WriteLine($"partial class {typeSymbol.Name}");
        using (codeGen.Scope())
        {
            codeGen.WriteLine($"public class {className} : global::NodeGraph.Model.Node");

            using (codeGen.Scope())
            {
                // JsonSerializerOptions
                codeGen.WriteLine("private static readonly global::System.Text.Json.JsonSerializerOptions JsonOptions = new global::System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };");
                codeGen.WriteLine();

                // スキーマ定数
                var escapedSchema = schemaJson.Replace("\"", "\"\"");
                codeGen.WriteLine($"private const string SchemaJson = @\"{escapedSchema}\";");
                codeGen.WriteLine();

                // フィールド宣言
                codeGen.WriteLine("private string _prompt = \"\";");
                codeGen.WriteLine("[global::NodeGraph.Model.PropertyAttribute(DisplayName = \"システムプロンプト\")]");
                codeGen.WriteLine("private string _systemPrompt = \"\";");

                foreach (var prop in properties)
                {
                    var defaultValue = GetDefaultValue(prop.TypeSymbol);
                    codeGen.WriteLine($"private {prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} _{ToCamelCase(prop.Name)}{defaultValue};");
                }

                codeGen.WriteLine("private bool _success;");
                codeGen.WriteLine("private string _error = \"\";");
                codeGen.WriteLine();

                // デフォルトコンストラクタ
                codeGen.WriteLine($"public {className}() : base({inputCount}, {outputCount}, {execInCount}, {execOutCount})");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("InputPorts[0] = new global::NodeGraph.Model.InputPort<string>(this, _prompt);");

                    var outputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"OutputPorts[{outputIndex}] = new global::NodeGraph.Model.OutputPort<{propType}>(this, _{ToCamelCase(prop.Name)});");
                        outputIndex++;
                    }

                    codeGen.WriteLine($"OutputPorts[{outputIndex}] = new global::NodeGraph.Model.OutputPort<bool>(this, _success);");
                    codeGen.WriteLine($"OutputPorts[{outputIndex + 1}] = new global::NodeGraph.Model.OutputPort<string>(this, _error);");

                    codeGen.WriteLine("ExecInPorts[0] = new global::NodeGraph.Model.ExecInPort(this);");
                    codeGen.WriteLine("ExecOutPorts[0] = new global::NodeGraph.Model.ExecOutPort(this);");
                }

                codeGen.WriteLine();

                // デシリアライズ用コンストラクタ
                codeGen.WriteLine($"public {className}(global::NodeGraph.Model.NodeId nodeId, global::NodeGraph.Model.PortId[] inputPortIds, global::NodeGraph.Model.PortId[] outputPortIds, global::NodeGraph.Model.PortId[] execInPortIds, global::NodeGraph.Model.PortId[] execOutPortIds) : base(nodeId, inputPortIds, outputPortIds, execInPortIds, execOutPortIds)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("InputPorts[0] = new global::NodeGraph.Model.InputPort<string>(this, inputPortIds[0], _prompt);");

                    var outputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"OutputPorts[{outputIndex}] = new global::NodeGraph.Model.OutputPort<{propType}>(this, outputPortIds[{outputIndex}], _{ToCamelCase(prop.Name)});");
                        outputIndex++;
                    }

                    codeGen.WriteLine($"OutputPorts[{outputIndex}] = new global::NodeGraph.Model.OutputPort<bool>(this, outputPortIds[{outputIndex}], _success);");
                    codeGen.WriteLine($"OutputPorts[{outputIndex + 1}] = new global::NodeGraph.Model.OutputPort<string>(this, outputPortIds[{outputIndex + 1}], _error);");

                    codeGen.WriteLine("ExecInPorts[0] = new global::NodeGraph.Model.ExecInPort(this, execInPortIds[0]);");
                    codeGen.WriteLine("ExecOutPorts[0] = new global::NodeGraph.Model.ExecOutPort(this, execOutPortIds[0]);");
                }

                codeGen.WriteLine();

                // BeforeExecute
                codeGen.WriteLine("protected override void BeforeExecute()");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("_prompt = ((global::NodeGraph.Model.InputPort<string>)InputPorts[0]).Value;");
                }

                codeGen.WriteLine();

                // AfterExecute
                codeGen.WriteLine("protected override void AfterExecute()");
                using (codeGen.Scope())
                {
                    var outputIndex = 0;
                    foreach (var prop in properties)
                    {
                        var propType = prop.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        codeGen.WriteLine($"((global::NodeGraph.Model.OutputPort<{propType}>)OutputPorts[{outputIndex}]).Value = _{ToCamelCase(prop.Name)};");
                        outputIndex++;
                    }

                    codeGen.WriteLine($"((global::NodeGraph.Model.OutputPort<bool>)OutputPorts[{outputIndex}]).Value = _success;");
                    codeGen.WriteLine($"((global::NodeGraph.Model.OutputPort<string>)OutputPorts[{outputIndex + 1}]).Value = _error;");
                }

                codeGen.WriteLine();

                // ExecuteCoreAsync
                codeGen.WriteLine("protected override async global::System.Threading.Tasks.Task ExecuteCoreAsync(global::NodeGraph.Model.NodeExecutionContext context)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("var chatClient = context.GetService<global::Microsoft.Extensions.AI.IChatClient>();");
                    codeGen.WriteLine("if (chatClient == null)");
                    using (codeGen.Scope())
                    {
                        codeGen.WriteLine("_success = false;");
                        codeGen.WriteLine("_error = \"IChatClient is not registered. Please set OPENAI_API_KEY parameter.\";");
                        codeGen.WriteLine("await context.ExecuteOutAsync(0);");
                        codeGen.WriteLine("return;");
                    }
                    codeGen.WriteLine();

                    codeGen.WriteLine("try");
                    using (codeGen.Scope())
                    {
                        codeGen.WriteLine("var messages = new global::System.Collections.Generic.List<global::Microsoft.Extensions.AI.ChatMessage>();");
                        codeGen.WriteLine();
                        codeGen.WriteLine("// システムプロンプト");
                        codeGen.WriteLine("var systemPromptText = string.IsNullOrEmpty(_systemPrompt)");
                        codeGen.WriteLine("    ? \"You must respond with valid JSON that matches the provided schema. Do not include any explanation or markdown formatting.\"");
                        codeGen.WriteLine("    : _systemPrompt + \"\\n\\nYou must respond with valid JSON that matches the provided schema. Do not include any explanation or markdown formatting.\";");
                        codeGen.WriteLine("messages.Add(new global::Microsoft.Extensions.AI.ChatMessage(global::Microsoft.Extensions.AI.ChatRole.System, systemPromptText));");
                        codeGen.WriteLine();
                        codeGen.WriteLine("// ユーザープロンプト + スキーマ");
                        codeGen.WriteLine("var userPromptText = _prompt + \"\\n\\nRespond with JSON matching this schema:\\n\" + SchemaJson;");
                        codeGen.WriteLine("messages.Add(new global::Microsoft.Extensions.AI.ChatMessage(global::Microsoft.Extensions.AI.ChatRole.User, userPromptText));");
                        codeGen.WriteLine();
                        codeGen.WriteLine("var response = await chatClient.GetResponseAsync(messages, cancellationToken: context.CancellationToken);");
                        codeGen.WriteLine("var jsonText = response.Text ?? \"\";");
                        codeGen.WriteLine();
                        codeGen.WriteLine("// JSON部分を抽出（```json ... ``` 形式の場合も対応）");
                        codeGen.WriteLine("jsonText = ExtractJson(jsonText);");
                        codeGen.WriteLine();
                        codeGen.WriteLine($"var obj = global::System.Text.Json.JsonSerializer.Deserialize<{fullTypeName}>(jsonText, JsonOptions);");
                        codeGen.WriteLine("if (obj != null)");
                        using (codeGen.Scope())
                        {
                            foreach (var prop in properties)
                                codeGen.WriteLine($"_{ToCamelCase(prop.Name)} = obj.{prop.Name};");
                            codeGen.WriteLine("_success = true;");
                            codeGen.WriteLine("_error = \"\";");
                        }

                        codeGen.WriteLine("else");
                        using (codeGen.Scope())
                        {
                            codeGen.WriteLine("_success = false;");
                            codeGen.WriteLine("_error = \"Deserialization returned null\";");
                        }
                    }

                    codeGen.WriteLine("catch (global::System.Text.Json.JsonException ex)");
                    using (codeGen.Scope())
                    {
                        codeGen.WriteLine("_success = false;");
                        codeGen.WriteLine("_error = \"JSON parse error: \" + ex.Message;");
                    }

                    codeGen.WriteLine("catch (global::System.Exception ex)");
                    using (codeGen.Scope())
                    {
                        codeGen.WriteLine("_success = false;");
                        codeGen.WriteLine("_error = ex.Message;");
                    }

                    codeGen.WriteLine("await context.ExecuteOutAsync(0);");
                }

                codeGen.WriteLine();

                // ExtractJson ヘルパーメソッド
                codeGen.WriteLine("private static string ExtractJson(string text)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("text = text.Trim();");
                    codeGen.WriteLine("// ```json ... ``` 形式の場合");
                    codeGen.WriteLine("if (text.StartsWith(\"```\"))");
                    using (codeGen.Scope())
                    {
                        codeGen.WriteLine("var startIndex = text.IndexOf('\\n');");
                        codeGen.WriteLine("if (startIndex >= 0)");
                        using (codeGen.Scope())
                        {
                            codeGen.WriteLine("var endIndex = text.LastIndexOf(\"```\");");
                            codeGen.WriteLine("if (endIndex > startIndex)");
                            using (codeGen.Scope())
                            {
                                codeGen.WriteLine("return text.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();");
                            }
                        }
                    }
                    codeGen.WriteLine("return text;");
                }

                codeGen.WriteLine();

                // ポート名取得メソッド
                EmitAiStructuredOutputPortNameMethods(codeGen, properties);

                // GetDisplayName
                codeGen.WriteLine($"public override string GetDisplayName() => \"{displayName} AI Output\";");

                // GetDirectory
                codeGen.WriteLine($"public override string GetDirectory() => \"{directory}\";");

                // GetProperties (PropertyAttribute対応)
                codeGen.WriteLine();
                codeGen.WriteLine("public override global::NodeGraph.Model.PropertyDescriptor[] GetProperties()");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("return new global::NodeGraph.Model.PropertyDescriptor[]");
                    codeGen.WriteLine("{");
                    codeGen.WriteLine("    new global::NodeGraph.Model.PropertyDescriptor");
                    codeGen.WriteLine("    {");
                    codeGen.WriteLine("        Name = \"SystemPrompt\",");
                    codeGen.WriteLine("        Type = typeof(string),");
                    codeGen.WriteLine("        Getter = obj => ((AiStructuredOutputNode)obj)._systemPrompt,");
                    codeGen.WriteLine("        Setter = (obj, val) => ((AiStructuredOutputNode)obj)._systemPrompt = (string)(val ?? \"\"),");
                    codeGen.WriteLine("        Attributes = new global::System.Attribute[] { new global::NodeGraph.Model.PropertyAttribute { DisplayName = \"システムプロンプト\" } }");
                    codeGen.WriteLine("    }");
                    codeGen.WriteLine("};");
                }
            }
        }

        var fileName = $"{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{typeSymbol.Name}.{className}.JsonNodeGenerator.g.cs";
        context.AddSource(fileName, codeGen.GetResult());
    }

    private static void EmitAiStructuredOutputPortNameMethods(CSharpCodeGenerator codeGen, List<JsonPropertyInfo> properties)
    {
        // GetInputPortName
        codeGen.WriteLine("public override string GetInputPortName(int index)");
        using (codeGen.Scope())
        {
            codeGen.WriteLine("return index == 0 ? \"Prompt\" : throw new global::System.InvalidOperationException(\"Invalid input port index\");");
        }

        // GetOutputPortName
        codeGen.WriteLine("public override string GetOutputPortName(int index)");
        using (codeGen.Scope())
        {
            codeGen.WriteLine("switch(index)");
            using (codeGen.Scope())
            {
                for (var i = 0; i < properties.Count; i++)
                    codeGen.WriteLine($"case {i}: return \"{properties[i].Name}\";");
                codeGen.WriteLine($"case {properties.Count}: return \"Success\";");
                codeGen.WriteLine($"case {properties.Count + 1}: return \"Error\";");
                codeGen.WriteLine("default: throw new global::System.InvalidOperationException(\"Invalid output port index\");");
            }
        }

        // GetExecInPortName
        codeGen.WriteLine("public override string GetExecInPortName(int index)");
        using (codeGen.Scope())
        {
            codeGen.WriteLine("return index == 0 ? \"ExecIn\" : throw new global::System.InvalidOperationException(\"Invalid exec in port index\");");
        }

        // GetExecOutPortName
        codeGen.WriteLine("public override string GetExecOutPortName(int index)");
        using (codeGen.Scope())
        {
            codeGen.WriteLine("return index == 0 ? \"Out\" : throw new global::System.InvalidOperationException(\"Invalid exec out port index\");");
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string GetDefaultValue(ITypeSymbol typeSymbol)
    {
        // 値型はデフォルト値が必要な場合
        if (typeSymbol.IsValueType) return ""; // デフォルト値で初期化される

        // 参照型はnull または空文字列
        if (typeSymbol.SpecialType == SpecialType.System_String) return " = \"\"";

        // nullable参照型
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated) return "";

        // その他の参照型
        return " = default!";
    }
}