namespace AdaptiveMusic.Models;

public sealed record DuckingState(
    bool Enabled,
    bool Ducking,
    bool MicrophoneActive,
    string RenderDeviceName,
    string CaptureDeviceName,
    IReadOnlyList<string> ActiveTriggers,
    IReadOnlyList<AudioSessionSnapshot> Sessions);
