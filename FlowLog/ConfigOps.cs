using System.IO;
using System.Text.Json;

namespace FlowLog
{
    public record AppConfig(
        string RemoteBare, // リモートリポジトリ
        string Actor,      // 名前
        string Email       // Emailアドレス
        );

    public static class ConfigOps
    {
        public static AppConfig? Load()
            => File.Exists(Paths.ConfigJson)
               ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(Paths.ConfigJson))
               : null;

        public static void Save(AppConfig cfg)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Paths.ConfigJson)!);
            File.WriteAllText(Paths.ConfigJson, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}