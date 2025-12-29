using System.Text.RegularExpressions;

namespace NodeGraph.Model;

[Node("Regex Extract", "String/Regex")]
public partial class RegexExtractNode
{
    [Input] private string _input = string.Empty;

    [Property(DisplayName = "Pattern", Tooltip = "正規表現パターン (キャプチャグループを含む)")]
    private string _pattern = string.Empty;

    [Property(DisplayName = "Ignore Case", Tooltip = "大文字小文字を無視するか")]
    private bool _ignoreCase;

    [Output] private bool _isMatch;
    [Output] private string _group0 = string.Empty;
    [Output] private string _group1 = string.Empty;
    [Output] private string _group2 = string.Empty;
    [Output] private string _group3 = string.Empty;

    private Regex? _cachedRegex;
    private string? _cachedPattern;
    private bool _cachedIgnoreCase;

    public void SetPattern(string pattern)
    {
        _pattern = pattern;
    }

    public void SetIgnoreCase(bool ignoreCase)
    {
        _ignoreCase = ignoreCase;
    }

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        var input = _input ?? string.Empty;
        var pattern = _pattern ?? string.Empty;

        _isMatch = false;
        _group0 = _group1 = _group2 = _group3 = string.Empty;

        if (!string.IsNullOrEmpty(pattern))
        {
            try
            {
                var regex = GetOrCreateRegex(pattern, _ignoreCase);
                var match = regex.Match(input);

                if (match.Success)
                {
                    _isMatch = true;
                    _group0 = match.Groups[0].Value;
                    if (match.Groups.Count > 1) _group1 = match.Groups[1].Value;
                    if (match.Groups.Count > 2) _group2 = match.Groups[2].Value;
                    if (match.Groups.Count > 3) _group3 = match.Groups[3].Value;
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern - leave outputs as empty
            }
        }

        await context.ExecuteOutAsync(0);
    }

    private Regex GetOrCreateRegex(string pattern, bool ignoreCase)
    {
        if (_cachedRegex != null &&
            _cachedPattern == pattern &&
            _cachedIgnoreCase == ignoreCase)
        {
            return _cachedRegex;
        }

        var options = RegexOptions.Compiled;
        if (ignoreCase) options |= RegexOptions.IgnoreCase;

        _cachedRegex = new Regex(pattern, options);
        _cachedPattern = pattern;
        _cachedIgnoreCase = ignoreCase;

        return _cachedRegex;
    }
}
