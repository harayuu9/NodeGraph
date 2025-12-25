using System.IO;
using NodeGraph.Model.Serialization;

namespace NodeGraph.Editor.Services;

public class ConfigService
{
    private const string ConfigFileName = "config.yml";
    private readonly Config _config;

    public ConfigService()
    {
        _config = YamlSerializer.Deserialize<Config>(ConfigFileName);;
    }

    public string HistoryDirectory => _config.HistoryDirectory;

    private class Config
    {
        public string HistoryDirectory { get; set; } = "history";
    }
}