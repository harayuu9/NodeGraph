using System.Collections.Generic;

namespace NodeGraph.Editor.Services;

/// <summary>
/// パラメータ辞書をマージするユーティリティ。
/// 優先度: runtimeParams > commonParams
/// </summary>
public static class ParameterMerger
{
    /// <summary>
    /// パラメータをマージします。runtimeParamsがcommonParamsを上書きします。
    /// </summary>
    /// <param name="commonParams">共通パラメータ（低優先度）</param>
    /// <param name="runtimeParams">実行時パラメータ（高優先度）</param>
    /// <returns>マージされたパラメータ辞書</returns>
    public static Dictionary<string, object?> Merge(
        IReadOnlyDictionary<string, object?>? commonParams,
        IReadOnlyDictionary<string, object?>? runtimeParams)
    {
        var result = new Dictionary<string, object?>();

        // 共通パラメータを追加（低優先度）
        if (commonParams != null)
        {
            foreach (var kvp in commonParams)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        // 実行時パラメータで上書き（高優先度）
        if (runtimeParams != null)
        {
            foreach (var kvp in runtimeParams)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }
}
