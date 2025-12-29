using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace NodeGraph.Editor.Services;

/// <summary>
/// DLLをスキャンして[Node]属性を持つ型が含まれているかを検査するクラス
/// ランタイムにロードせずにメタデータのみを読み取る
/// </summary>
public class PluginScanner
{
    private const string NodeAttributeFullName = "NodeGraph.Model.NodeAttribute";

    /// <summary>
    /// DLLに[Node]属性を持つ型が含まれているかを検査する
    /// </summary>
    /// <param name="dllPath">検査するDLLのパス</param>
    /// <returns>[Node]属性を持つ型が存在する場合はtrue</returns>
    public bool ContainsNodeTypes(string dllPath)
    {
        try
        {
            using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var peReader = new PEReader(fs);

            if (!peReader.HasMetadata)
                return false;

            var metadataReader = peReader.GetMetadataReader();

            // 全てのカスタム属性をスキャン
            foreach (var attrHandle in metadataReader.CustomAttributes)
            {
                var attr = metadataReader.GetCustomAttribute(attrHandle);
                if (IsNodeAttribute(metadataReader, attr))
                    return true;
            }

            return false;
        }
        catch
        {
            // ファイルアクセスエラー、無効なPE形式などはfalseを返す
            return false;
        }
    }

    /// <summary>
    /// 指定されたカスタム属性がNodeAttributeかどうかを判定する
    /// </summary>
    private static bool IsNodeAttribute(MetadataReader reader, CustomAttribute attr)
    {
        try
        {
            // コンストラクタ参照から型を取得
            if (attr.Constructor.Kind == HandleKind.MemberReference)
            {
                var memberRef = reader.GetMemberReference((MemberReferenceHandle)attr.Constructor);
                if (memberRef.Parent.Kind == HandleKind.TypeReference)
                {
                    var typeRef = reader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
                    var fullName = GetFullTypeName(reader, typeRef);
                    return fullName == NodeAttributeFullName;
                }
            }
            else if (attr.Constructor.Kind == HandleKind.MethodDefinition)
            {
                var methodDef = reader.GetMethodDefinition((MethodDefinitionHandle)attr.Constructor);
                var typeDef = reader.GetTypeDefinition(methodDef.GetDeclaringType());
                var fullName = GetFullTypeName(reader, typeDef);
                return fullName == NodeAttributeFullName;
            }
        }
        catch
        {
            // メタデータ読み取りエラーは無視
        }

        return false;
    }

    /// <summary>
    /// TypeReferenceからフルネームを取得
    /// </summary>
    private static string GetFullTypeName(MetadataReader reader, TypeReference typeRef)
    {
        var typeName = reader.GetString(typeRef.Name);
        var typeNamespace = reader.GetString(typeRef.Namespace);
        return string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}";
    }

    /// <summary>
    /// TypeDefinitionからフルネームを取得
    /// </summary>
    private static string GetFullTypeName(MetadataReader reader, TypeDefinition typeDef)
    {
        var typeName = reader.GetString(typeDef.Name);
        var typeNamespace = reader.GetString(typeDef.Namespace);
        return string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}";
    }

    /// <summary>
    /// 指定されたディレクトリ内のすべてのDLLファイルを列挙する
    /// </summary>
    /// <param name="pluginsDirectory">プラグインディレクトリのパス</param>
    /// <returns>DLLファイルのパスの列挙</returns>
    public IEnumerable<string> EnumerateDlls(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
            yield break;

        foreach (var dll in Directory.EnumerateFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories))
            yield return dll;
    }
}
