using System.Text.RegularExpressions;

namespace NodeGraph.Model;

[Node("Regex Match", "String/Regex")]
public partial class RegexMatchNode
{
    [Input] private string _input = string.Empty;

    [Property(DisplayName = "Pattern", Tooltip = "正規表現パターン")]
    private string _pattern = string.Empty;

    [Property(DisplayName = "Ignore Case", Tooltip = "大文字小文字を無視するか")]
    private bool _ignoreCase;

    [Output] private bool _isMatch;
    [Output] private string _matchValue = string.Empty;

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

        if (string.IsNullOrEmpty(pattern))
        {
            _isMatch = false;
            _matchValue = string.Empty;
        }
        else
        {
            try
            {
                var regex = GetOrCreateRegex(pattern, _ignoreCase);
                var match = regex.Match(input);
                _isMatch = match.Success;
                _matchValue = match.Success ? match.Value : string.Empty;
            }
            catch (ArgumentException)
            {
                _isMatch = false;
                _matchValue = string.Empty;
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
