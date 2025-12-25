using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NodeGraph.Model.Serialization;

public static class YamlSerializer
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static string Serialize<T>(T obj) => Serializer.Serialize(obj);
    public static T Deserialize<T>(string yaml) => Deserializer.Deserialize<T>(yaml);

    public static T LoadFile<T>(string filePath) where T : new()
    {
        if (File.Exists(filePath))
        {
            try
            {
                return Deserializer.Deserialize<T>(File.ReadAllText(filePath));
            }
            catch (Exception e)
            {
                return new T();
            }
        }

        return new T();
    }
}