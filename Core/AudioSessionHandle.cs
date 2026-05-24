using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AdaptiveMusic.Core;

internal sealed class AudioSessionHandle
{
    private readonly AudioSessionControl _control;

    public AudioSessionHandle(AudioSessionControl control)
    {
        _control = control;
        Id = SafeRead(() => _control.GetSessionInstanceIdentifier, Guid.NewGuid().ToString("N"));
        ProcessId = (int)SafeRead(() => _control.GetProcessID, 0U);
    }

    public string Id { get; }
    public int ProcessId { get; }

    public string ProcessName => ProcessId <= 0
        ? "System"
        : SafeRead(() => System.Diagnostics.Process.GetProcessById(ProcessId).ProcessName, $"PID {ProcessId}");

    public string DisplayName => SafeRead(() => _control.DisplayName, string.Empty);
    public bool IsSystemSounds => SafeRead(() => _control.IsSystemSoundsSession, false);
    public AudioSessionState State => SafeRead(() => _control.State, AudioSessionState.AudioSessionStateExpired);
    public float Peak => SafeRead(() => _control.AudioMeterInformation.MasterPeakValue, 0f);
    public float Volume => SafeRead(() => _control.SimpleAudioVolume.Volume, 0f);
    public bool Muted => SafeRead(() => _control.SimpleAudioVolume.Mute, false);

    public bool IsExpired => State == AudioSessionState.AudioSessionStateExpired;

    public void SetVolume(float volume)
    {
        if (IsExpired)
        {
            return;
        }

        _control.SimpleAudioVolume.Volume = Math.Clamp(volume, 0f, 1f);
    }

    public void Dispose()
    {
        _control.Dispose();
    }

    private static T SafeRead<T>(Func<T> read, T fallback)
    {
        try
        {
            return read();
        }
        catch
        {
            return fallback;
        }
    }
}
