namespace AdaptiveMusic.Models;

public sealed record AudioSessionSnapshot(
    string Id,
    int ProcessId,
    string ProcessName,
    string DisplayName,
    float Peak,
    float Volume,
    bool Muted,
    bool IsMusicTarget,
    bool IsTrigger,
    bool IsSystemSounds);
