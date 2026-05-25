using AdaptiveMusic.Configuration;

namespace AdaptiveMusic.Tests;

public class AppConfigTests
{
    [Fact]
    public void NewInstance_HasSensibleDefaults()
    {
        var config = new AppConfig();

        Assert.True(config.Enabled);
        Assert.Contains("Spotify", config.MusicProcesses);
        Assert.Equal(0.10f, config.DuckVolume);
        Assert.Equal(1500, config.RestoreDelayMs);
        Assert.True(config.UseFade);
        Assert.True(config.DuckOnMicrophone);
    }

    [Fact]
    public void Save_Then_LoadOrCreate_PreservesValues()
    {
        var original = new AppConfig
        {
            DuckVolume = 0.3f,
            PollIntervalMs = 200,
            ThemeMode = "Dark"
        };
        original.Save();

        var loaded = AppConfig.LoadOrCreate();

        Assert.Equal(0.3f, loaded.DuckVolume);
        Assert.Equal(200, loaded.PollIntervalMs);
        Assert.Equal("Dark", loaded.ThemeMode);
    }

    [Fact]
    public void LoadOrCreate_WithInvalidJson_BacksUpAndReturnsNewConfig()
    {
        File.WriteAllText(AppConfig.ConfigPath, "this is not json");

        var loaded = AppConfig.LoadOrCreate();

        Assert.True(loaded.Enabled);
        Assert.Equal(0.10f, loaded.DuckVolume);

        // Backup file should exist
        var backupFiles = Directory.GetFiles(AppConfig.ConfigDirectory, "config.invalid.*.json");
        Assert.Single(backupFiles);

        // Clean up backup
        foreach (var f in backupFiles)
            File.Delete(f);
    }

    [Fact]
    public void Save_CreatesDirectoryIfMissing()
    {
        if (Directory.Exists(AppConfig.ConfigDirectory))
            Directory.Delete(AppConfig.ConfigDirectory, recursive: true);

        var config = new AppConfig();
        config.Save();

        Assert.True(File.Exists(AppConfig.ConfigPath));

        // Reload to confirm valid JSON
        var reloaded = AppConfig.LoadOrCreate();
        Assert.NotNull(reloaded);
    }

    [Fact]
    public void MusicProcesses_DefaultList_ContainsExpectedApps()
    {
        var config = new AppConfig();
        Assert.Contains("Spotify", config.MusicProcesses);
        Assert.Contains("cloudmusic", config.MusicProcesses);
        Assert.Contains("QQMusic", config.MusicProcesses);
    }
}
