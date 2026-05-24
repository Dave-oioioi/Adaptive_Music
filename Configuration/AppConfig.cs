using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdaptiveMusic.Configuration;

public sealed class AppConfig
{
    public bool Enabled { get; set; } = true;
    public List<string> MusicProcesses { get; set; } = ["Spotify", "cloudmusic", "QQMusic", "foobar2000", "MusicBee", "AIMP"];
    public List<string> IgnoredTriggerProcesses { get; set; } = ["AdaptiveMusic"];
    public float DuckVolume { get; set; } = 0.25f;
    public float TriggerThreshold { get; set; } = 0.015f;
    public float MicrophoneThreshold { get; set; } = 0.02f;
    public int RestoreDelayMs { get; set; } = 1500;
    public int PollIntervalMs { get; set; } = 150;
    public int FadeStepMs { get; set; } = 35;
    public bool UseFade { get; set; } = true;
    public int FadeDurationMs { get; set; } = 280;
    public bool DuckOnMicrophone { get; set; } = true;
    public bool DuckOnTyping { get; set; } = true;
    public List<string> TypingTriggerProcesses { get; set; } = ["TextInputHost"];
    public Dictionary<string, float> NormalMusicVolumes { get; set; } = [];
    public string ThemeMode { get; set; } = "System";

    [JsonIgnore]
    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdaptiveMusic");

    [JsonIgnore]
    public static string ConfigPath => Path.Combine(ConfigDirectory, "config.json");

    public static AppConfig LoadOrCreate()
    {
        Directory.CreateDirectory(ConfigDirectory);

        if (!File.Exists(ConfigPath))
        {
            var created = new AppConfig();
            created.Save();
            return created;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions()) ?? new AppConfig();
        }
        catch
        {
            var backupPath = Path.Combine(ConfigDirectory, $"config.invalid.{DateTime.Now:yyyyMMddHHmmss}.json");
            File.Copy(ConfigPath, backupPath, overwrite: true);
            var created = new AppConfig();
            created.Save();
            return created;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(this, JsonOptions());
        File.WriteAllText(ConfigPath, json);
    }

    public void OpenInEditor()
    {
        Save();
        Process.Start(new ProcessStartInfo
        {
            FileName = ConfigPath,
            UseShellExecute = true
        });
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}
