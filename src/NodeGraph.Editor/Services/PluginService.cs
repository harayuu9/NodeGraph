using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NodeGraph.Model;

namespace NodeGraph.Editor.Services;

/// <summary>
/// プラグインの発見、ロード、登録を統括するサービス
/// </summary>
public class PluginService
{
    private readonly List<PluginLoadResult> _loadResults = [];
    private readonly List<Type> _pluginNodeTypes = [];
    private readonly HashSet<string> _loadedAssemblyNames = [];

    /// <summary>
    /// プラグインロード結果のリスト
    /// </summary>
    public IReadOnlyList<PluginLoadResult> LoadResults => _loadResults;

    /// <summary>
    /// 発見されたプラグインノードタイプのリスト
    /// </summary>
    public IReadOnlyList<Type> PluginNodeTypes => _pluginNodeTypes;

    /// <summary>
    /// 指定されたディレクトリからプラグインをロードする
    /// </summary>
    /// <param name="pluginsDirectory">プラグインディレクトリのパス</param>
    public void LoadPlugins(string pluginsDirectory)
    {
        _loadResults.Clear();
        _pluginNodeTypes.Clear();

        var scanner = new PluginScanner();

        // 既にロード済みのアセンブリ名を収集（重複チェック用）
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.IsDynamic)
                _loadedAssemblyNames.Add(asm.GetName().Name ?? string.Empty);
        }

        // プラグインディレクトリ内のすべてのDLLをスキャン
        foreach (var dllPath in scanner.EnumerateDlls(pluginsDirectory))
        {
            var result = TryLoadPlugin(dllPath, pluginsDirectory, scanner);
            _loadResults.Add(result);

            if (result.Success)
            {
                Debug.WriteLine($"[PluginService] Loaded plugin: {dllPath} ({result.DiscoveredNodeTypes.Count} nodes)");
            }
            else
            {
                Debug.WriteLine($"[PluginService] Skipped: {dllPath} - {result.ErrorMessage}");
            }
        }
    }

    /// <summary>
    /// 単一のDLLをプラグインとしてロードを試みる
    /// </summary>
    private PluginLoadResult TryLoadPlugin(string dllPath, string pluginsDirectory, PluginScanner scanner)
    {
        try
        {
            // Step 1: メタデータ検査（ロードせずに[Node]属性をチェック）
            if (!scanner.ContainsNodeTypes(dllPath))
            {
                return new PluginLoadResult
                {
                    DllPath = dllPath,
                    Success = false,
                    ErrorMessage = "No [Node] types found"
                };
            }

            // Step 2: 重複チェック
            var assemblyName = AssemblyName.GetAssemblyName(dllPath);
            if (_loadedAssemblyNames.Contains(assemblyName.Name ?? string.Empty))
            {
                return new PluginLoadResult
                {
                    DllPath = dllPath,
                    Success = false,
                    ErrorMessage = $"Assembly '{assemblyName.Name}' is already loaded"
                };
            }

            // Step 3: カスタムコンテキストでアセンブリをロード
            var loadContext = new PluginAssemblyLoadContext(dllPath, pluginsDirectory);
            var assembly = loadContext.LoadFromAssemblyPath(dllPath);

            _loadedAssemblyNames.Add(assemblyName.Name ?? string.Empty);

            // Step 4: ノードタイプを発見
            var nodeTypes = DiscoverNodeTypes(assembly);
            _pluginNodeTypes.AddRange(nodeTypes);

            return new PluginLoadResult
            {
                DllPath = dllPath,
                Success = true,
                DiscoveredNodeTypes = nodeTypes
            };
        }
        catch (Exception ex)
        {
            return new PluginLoadResult
            {
                DllPath = dllPath,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// アセンブリからNodeを継承する非抽象クラスを発見する
    /// </summary>
    private static List<Type> DiscoverNodeTypes(Assembly assembly)
    {
        var nodeBaseType = typeof(Node);
        var result = new List<Type>();

        try
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && type.IsClass && nodeBaseType.IsAssignableFrom(type))
                    result.Add(type);
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // 一部の型のみロードできた場合は、ロードできた型のみ処理
            foreach (var type in ex.Types)
            {
                if (type != null && !type.IsAbstract && type.IsClass && nodeBaseType.IsAssignableFrom(type))
                    result.Add(type);
            }
        }

        return result;
    }
}
