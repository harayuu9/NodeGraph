using System.Text.Json;

namespace NodeGraph.Model;

/// <summary>
/// JSON文字列から指定したパスの値を抽出するノード
/// </summary>
[Node("JSON Parse", "IO/Json")]
public partial class JsonParseNode
{
    [Input] private string _json = string.Empty;
    [Input] private string _path = string.Empty;
    [Output] private string _value = string.Empty;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        if (string.IsNullOrEmpty(_json))
        {
            _value = string.Empty;
            await context.ExecuteOutAsync(0);
            return;
        }

        using var doc = JsonDocument.Parse(_json);
        _value = GetValueByPath(doc.RootElement, _path);
        await context.ExecuteOutAsync(0);
    }

    /// <summary>
    /// JSONパスから値を取得する
    /// パス例: "data.items[0].name" or "items[2]" or "name"
    /// </summary>
    private static string GetValueByPath(JsonElement element, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return element.ToString();
        }

        var current = element;
        var segments = ParsePath(path);

        foreach (var segment in segments)
        {
            if (segment.IsArrayIndex)
            {
                if (current.ValueKind != JsonValueKind.Array)
                    return string.Empty;

                if (segment.Index < 0 || segment.Index >= current.GetArrayLength())
                    return string.Empty;

                current = current[segment.Index];
            }
            else
            {
                if (current.ValueKind != JsonValueKind.Object)
                    return string.Empty;

                if (!current.TryGetProperty(segment.Name, out var next))
                    return string.Empty;

                current = next;
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => current.GetRawText()
        };
    }

    private static List<PathSegment> ParsePath(string path)
    {
        var segments = new List<PathSegment>();
        var i = 0;

        while (i < path.Length)
        {
            // Skip dots
            if (path[i] == '.')
            {
                i++;
                continue;
            }

            // Array index
            if (path[i] == '[')
            {
                var endBracket = path.IndexOf(']', i);
                if (endBracket == -1) break;

                var indexStr = path.Substring(i + 1, endBracket - i - 1);
                if (int.TryParse(indexStr, out var index))
                {
                    segments.Add(new PathSegment { IsArrayIndex = true, Index = index });
                }
                i = endBracket + 1;
                continue;
            }

            // Property name
            var start = i;
            while (i < path.Length && path[i] != '.' && path[i] != '[')
            {
                i++;
            }

            if (i > start)
            {
                segments.Add(new PathSegment { IsArrayIndex = false, Name = path.Substring(start, i - start) });
            }
        }

        return segments;
    }

    private struct PathSegment
    {
        public bool IsArrayIndex;
        public int Index;
        public string Name;
    }
}
