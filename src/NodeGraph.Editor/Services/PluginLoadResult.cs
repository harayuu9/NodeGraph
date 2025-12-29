using System;
using System.Collections.Generic;

namespace NodeGraph.Editor.Services;

/// <summary>
/// プラグインロード操作の結果を表すクラス
/// </summary>
public class PluginLoadResult
{
    /// <summary>
    /// ロードを試みたDLLのパス
    /// </summary>
    public required string DllPath { get; init; }

    /// <summary>
    /// ロードが成功したかどうか
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ設定）
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 発見されたノードタイプのリスト
    /// </summary>
    public IReadOnlyList<Type> DiscoveredNodeTypes { get; init; } = [];
}
