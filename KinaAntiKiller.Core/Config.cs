using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace AntiKiller.Core
{
    public class Config
    {
        public static readonly JsonSerializerOptions DefaultSerializerOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };
        private static Config _instance;

        public static Config Instance { get { _instance ??= Load(); return _instance; } }
        public static string ConfigPath => Path.Combine(Environment.CurrentDirectory, "Config.json");
        public static Config Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = JsonNode.Parse(File.ReadAllText(ConfigPath)); //为了对付裁剪
                    var config = new Config()
                    {
                        UId = json[nameof(UId)]?.GetValue<long>() ?? 0,
                        Cookie = json[nameof(Cookie)]?.GetValue<string>() ?? string.Empty,
                        Block = json[nameof(Block)]?.GetValue<bool>() ?? default,
                    };
                    return config;
                }
                catch
                {
                    Logs.Warn($"配置文件读取失败");
                    return new();
                }
            }
            else
            {
                var config = new Config();
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, DefaultSerializerOptions));
                return config;
            }
        }
        public long UId { get; set; }
        public string Cookie { get; set; } = "";
        public bool Block { get; set; } = false;
    }
}
