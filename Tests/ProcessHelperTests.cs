using AdaptiveMusic.Core;

namespace AdaptiveMusic.Tests;

public class ProcessHelperTests
{
    [Theory]
    [InlineData("Spotify.exe", "Spotify")]
    [InlineData("spotify", "spotify")]
    [InlineData("cloudmusic.exe", "cloudmusic")]
    [InlineData("QQMusic", "QQMusic")]
    [InlineData("C:\\Program Files\\App\\music.exe", "music")]
    [InlineData("", "")]
    public void NormalizeProcessName_StripsExtension(string input, string expected)
    {
        var result = AdaptiveMusicService.NormalizeProcessName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Spotify", "Spotify", true)]
    [InlineData("spotify", "SPOTIFY", true)]
    [InlineData("Spotify", "spotify", true)]
    [InlineData("Spotify", "NotSpotify", false)]
    [InlineData("cloudmusic", "QQMusic", false)]
    [InlineData("", "", true)]
    public void ProcessMatches_IsCaseInsensitive(string processName, string pattern, bool expected)
    {
        var result = AdaptiveMusicService.ProcessMatches(processName, pattern);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeProcessName_HandlesNullOrWhitespace()
    {
        var result = AdaptiveMusicService.NormalizeProcessName("   ");
        Assert.Equal("", result);
    }
}
