using System;
using NodeGraph.Editor;
using NodeGraph.Model;

namespace NodeGraph.Sandbox;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        EditorEntryPoint.Run(args);
    }
}

/// <summary>
/// テスト用のPersonクラス
/// </summary>
[JsonNode(DisplayName = "Person", Directory = "Json/Test")]
public partial class Person
{
    [JsonProperty(Description = "名前")]
    public string Name { get; set; } = "";

    [JsonProperty(Description = "年齢")]
    public int Age { get; set; }

    [JsonProperty(Description = "メールアドレス", Required = false)]
    public string? Email { get; set; }
}