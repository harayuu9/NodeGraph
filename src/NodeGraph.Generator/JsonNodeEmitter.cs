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
        var className = $"{typeSymbol.Name}DeserializeNode";
        var inputCount = 1; // _json
        var outputCount = properties.Count + 2; // properties + _success + _error
        var execInCount = 1;
        var execOutCount = 1;

        var codeGen = new CSharpCodeGenerator(ns);
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
                        foreach (var prop in properties)
                        {
                            codeGen.WriteLine($"_{ToCamelCase(prop.Name)} = obj.{prop.Name};");
                        }
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
        }

        var fileName = $"{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{className}.JsonNodeGenerator.g.cs";
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
        var className = $"{typeSymbol.Name}SerializeNode";
        var inputCount = properties.Count;
        var outputCount = 1; // _json
        var execInCount = 1;
        var execOutCount = 1;

        var codeGen = new CSharpCodeGenerator(ns);
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
                foreach (var prop in properties)
                {
                    codeGen.WriteLine($"obj.{prop.Name} = _{ToCamelCase(prop.Name)};");
                }
                codeGen.WriteLine("_json = global::System.Text.Json.JsonSerializer.Serialize(obj, JsonOptions);");
                codeGen.WriteLine("await context.ExecuteOutAsync(0);");
            }
            codeGen.WriteLine();

            // ポート名取得メソッド
            EmitPortNameMethods(codeGen, properties, false);
        }

        var fileName = $"{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{className}.JsonNodeGenerator.g.cs";
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
        var className = $"{typeSymbol.Name}SchemaNode";

        var codeGen = new CSharpCodeGenerator(ns);
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
            codeGen.WriteLine("protected override void AfterExecute() { }");
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
        }

        var fileName = $"{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{className}.JsonNodeGenerator.g.cs";
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
                    for (var i = 0; i < properties.Count; i++)
                    {
                        codeGen.WriteLine($"case {i}: return \"{properties[i].Name}\";");
                    }
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
                    for (var i = 0; i < properties.Count; i++)
                    {
                        codeGen.WriteLine($"case {i}: return \"{properties[i].Name}\";");
                    }
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

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string GetDefaultValue(ITypeSymbol typeSymbol)
    {
        // 値型はデフォルト値が必要な場合
        if (typeSymbol.IsValueType)
        {
            return ""; // デフォルト値で初期化される
        }

        // 参照型はnull または空文字列
        if (typeSymbol.SpecialType == SpecialType.System_String)
        {
            return " = \"\"";
        }

        // nullable参照型
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return "";
        }

        // その他の参照型
        return " = default!";
    }
}
