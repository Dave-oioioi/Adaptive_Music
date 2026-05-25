using AdaptiveMusic.Models;

namespace AdaptiveMusic.Tests;

public class ModelTests
{
    [Fact]
    public void DuckingState_Creation_StoresAllProperties()
    {
        var snapshot = new AudioSessionSnapshot(
            "id-1", 1234, "Spotify", "Spotify - Now Playing",
            0.1f, 0.8f, false, true, false, false);

        var state = new DuckingState(
            true, false, true,
            "Speakers", "Microphone",
            ["TextInputHost"],
            [snapshot]);

        Assert.True(state.Enabled);
        Assert.False(state.Ducking);
        Assert.True(state.MicrophoneActive);
        Assert.Equal("Speakers", state.RenderDeviceName);
        Assert.Equal("Microphone", state.CaptureDeviceName);
        Assert.Single(state.ActiveTriggers);
        Assert.Single(state.Sessions);
    }

    [Fact]
    public void DuckingState_WithExpression_ClonesCorrectly()
    {
        var state = new DuckingState(true, false, false, "A", "B", [], []);
        var modified = state with { Ducking = true, RenderDeviceName = "C" };

        Assert.True(modified.Ducking);
        Assert.Equal("C", modified.RenderDeviceName);
        Assert.Equal("B", modified.CaptureDeviceName); // unchanged
    }

    [Fact]
    public void AudioSessionSnapshot_AllPropertiesMapped()
    {
        var snapshot = new AudioSessionSnapshot(
            "session-42", 9999, "foobar2000", "foobar2000 - Track Title",
            0.75f, 0.5f, true, true, false, true);

        Assert.Equal("session-42", snapshot.Id);
        Assert.Equal(9999, snapshot.ProcessId);
        Assert.Equal("foobar2000", snapshot.ProcessName);
        Assert.Equal("foobar2000 - Track Title", snapshot.DisplayName);
        Assert.Equal(0.75f, snapshot.Peak);
        Assert.Equal(0.5f, snapshot.Volume);
        Assert.True(snapshot.Muted);
        Assert.True(snapshot.IsMusicTarget);
        Assert.False(snapshot.IsTrigger);
        Assert.True(snapshot.IsSystemSounds);
    }
}
