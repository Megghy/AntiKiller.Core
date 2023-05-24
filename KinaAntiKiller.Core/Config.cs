using System.ComponentModel;
using Newtonsoft.Json;

namespace AntiKiller.Core
{
    public class Config
    {
        private static Config _instance;
        public static Config Instance { get { _instance ??= Load(); return _instance; } }
        public static string ConfigPath => Path.Combine(Environment.CurrentDirectory, "Config.json");
        public static Config Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
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
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
                return config;
            }
        }
        public long UId { get; set; }
        public string Cookie { get; set; } = "";
    }
}
