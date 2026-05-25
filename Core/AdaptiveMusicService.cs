using AdaptiveMusic.Configuration;
using AdaptiveMusic.Models;
using NAudio.CoreAudioApi;

namespace AdaptiveMusic.Core;

public sealed class AdaptiveMusicService : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Dictionary<string, AudioSessionHandle> _sessions = [];
    private readonly Dictionary<string, float> _originalVolumes = [];
    private readonly HashSet<string> _restoringSessions = [];
    private readonly Dictionary<string, CancellationTokenSource> _fadeJobs = [];
    private MMDeviceEnumerator? _enumerator;
    private MMDevice? _renderDevice;
    private MMDevice? _captureDevice;
    private DateTime _lastTriggerUtc = DateTime.MinValue;
    private bool _ducking;
    private bool _disposed;

    public AdaptiveMusicService(AppConfig config)
    {
        Config = config;
        State = EmptyState();
        _timer = new System.Windows.Forms.Timer { Interval = Math.Max(50, config.PollIntervalMs) };
        _timer.Tick += (_, _) => Tick();
    }

    public AppConfig Config { get; private set; }
    public DuckingState State { get; private set; }
    public event EventHandler<DuckingState>? StateChanged;

    public IReadOnlyList<string> GetAudibleProcessNames(float? threshold = null)
    {
        EnsureDevices();
        RefreshSessions();
        var minPeak = threshold ?? Config.TriggerThreshold;

        return _sessions.Values
            .Where(session => !session.Muted && !session.IsSystemSounds && session.Peak >= minPeak)
            .Select(session => NormalizeProcessName(session.ProcessName))
            .Where(name => !string.IsNullOrWhiteSpace(name) && !ProcessMatches(name, "AdaptiveMusic"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();
    }

    public IReadOnlyList<string> GetMixerProcessNames()
    {
        EnsureDevices();
        RefreshSessions();

        return _sessions.Values
            .Where(session => !session.IsSystemSounds)
            .Select(session => NormalizeProcessName(session.ProcessName))
            .Where(name => !string.IsNullOrWhiteSpace(name) && !ProcessMatches(name, "AdaptiveMusic"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();
    }

    public void Start()
    {
        EnsureDevices();
        RefreshSessions();
        HealRememberedMusicVolumes();
        _timer.Start();
        Tick();
    }

    public void Stop()
    {
        _timer.Stop();
        RestoreTargets(force: true);
    }

    public void ReloadConfig(AppConfig config)
    {
        Config = config;
        _timer.Interval = Math.Max(50, config.PollIntervalMs);
        RestoreTargets(force: true);
        Tick();
    }

    private void Tick()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            EnsureDevices();
            RefreshSessions();
            ApplyRules();
        }
        catch
        {
            ResetDevices();
            State = State with { RenderDeviceName = "Audio device unavailable" };
            StateChanged?.Invoke(this, State);
        }
    }

    private void EnsureDevices()
    {
        _enumerator ??= new MMDeviceEnumerator();

        if (_renderDevice is null || _renderDevice.State != DeviceState.Active)
        {
            _renderDevice?.Dispose();
            _renderDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        if (!Config.DuckOnMicrophone)
        {
            _captureDevice?.Dispose();
            _captureDevice = null;
            return;
        }

        try
        {
            if (_captureDevice is null || _captureDevice.State != DeviceState.Active)
            {
                _captureDevice?.Dispose();
                _captureDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            }
        }
        catch
        {
            _captureDevice?.Dispose();
            _captureDevice = null;
        }
    }

    private void RefreshSessions()
    {
        if (_renderDevice is null)
        {
            return;
        }

        _renderDevice.AudioSessionManager.RefreshSessions();
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sessions = _renderDevice.AudioSessionManager.Sessions;

        for (var index = 0; index < sessions.Count; index++)
        {
            var control = sessions[index];
            var id = SafeRead(() => control.GetSessionInstanceIdentifier, Guid.NewGuid().ToString("N"));
            activeIds.Add(id);

            if (!_sessions.ContainsKey(id))
            {
                _sessions[id] = new AudioSessionHandle(control);
            }
            else
            {
                control.Dispose();
            }
        }

        foreach (var staleId in _sessions.Keys.Where(id => !activeIds.Contains(id) || _sessions[id].IsExpired).ToList())
        {
            _sessions[staleId].Dispose();
            _sessions.Remove(staleId);
            _originalVolumes.Remove(staleId);
            CancelFade(staleId);
        }
    }

    private void ApplyRules()
    {
        var snapshots = new List<AudioSessionSnapshot>();
        var triggers = new List<string>();
        var musicTargets = new List<AudioSessionHandle>();
        var microphoneActive = IsMicrophoneActive();

        foreach (var session in _sessions.Values.ToList())
        {
            var processName = NormalizeProcessName(session.ProcessName);
            var isMusic = IsConfiguredMusicTarget(session);
            var ignored = Config.IgnoredTriggerProcesses.Any(target => ProcessMatches(processName, target));
            var isTypingTrigger = Config.TypingTriggerProcesses.Any(target => ProcessMatches(processName, target));
            var isTrigger = Config.Enabled
                && !isMusic
                && !ignored
                && (Config.DuckOnTyping || !isTypingTrigger)
                && !session.IsSystemSounds
                && !session.Muted
                && session.Peak >= Config.TriggerThreshold;

            if (isMusic && !session.Muted)
            {
                musicTargets.Add(session);
            }

            if (isTrigger)
            {
                triggers.Add(LabelFor(session));
            }

            snapshots.Add(new AudioSessionSnapshot(
                session.Id,
                session.ProcessId,
                processName,
                session.DisplayName,
                session.Peak,
                session.Volume,
                session.Muted,
                isMusic,
                isTrigger,
                session.IsSystemSounds));
        }

        var shouldDuck = Config.Enabled && musicTargets.Count > 0 && (triggers.Count > 0 || microphoneActive);
        if (shouldDuck)
        {
            _lastTriggerUtc = DateTime.UtcNow;
            DuckTargets(musicTargets);
        }
        else if ((_ducking || _originalVolumes.Count > 0) && DateTime.UtcNow - _lastTriggerUtc >= TimeSpan.FromMilliseconds(Config.RestoreDelayMs))
        {
            RestoreTargets(force: false);
        }

        State = new DuckingState(
            Config.Enabled,
            _ducking,
            microphoneActive,
            _renderDevice?.FriendlyName ?? "No output device",
            _captureDevice?.FriendlyName ?? "No microphone device",
            triggers,
            snapshots.OrderByDescending(s => s.IsTrigger).ThenByDescending(s => s.IsMusicTarget).ThenBy(s => s.ProcessName).ToList());

        StateChanged?.Invoke(this, State);
    }

    private void DuckTargets(IEnumerable<AudioSessionHandle> targets)
    {
        _ducking = true;

        foreach (var target in targets)
        {
            if (!CanWriteSessionVolume(target))
            {
                continue;
            }

            if (target.Volume <= 0.02f)
            {
                continue;
            }

            if (!_originalVolumes.ContainsKey(target.Id))
            {
                _originalVolumes[target.Id] = ResolveOriginalVolume(target);
            }

            _restoringSessions.Remove(target.Id);
            var destination = Math.Min(_originalVolumes[target.Id], Config.DuckVolume);
            if (Math.Abs(target.Volume - destination) > 0.01f)
            {
                FadeTo(target, destination);
            }
        }
    }

    private void RestoreTargets(bool force)
    {
        if (!_ducking && !force && _originalVolumes.Count == 0)
        {
            return;
        }

        foreach (var pair in _originalVolumes.ToList())
        {
            if (!_sessions.TryGetValue(pair.Key, out var session))
            {
                _originalVolumes.Remove(pair.Key);
                continue;
            }

            if (!CanWriteSessionVolume(session))
            {
                _originalVolumes.Remove(pair.Key);
                _restoringSessions.Remove(pair.Key);
                continue;
            }

            if (force)
            {
                CancelFade(session.Id);
                session.SetVolume(pair.Value);
                _originalVolumes.Remove(pair.Key);
                _restoringSessions.Remove(pair.Key);
                RememberNormalVolume(session, pair.Value);
            }
            else if (!_restoringSessions.Contains(pair.Key) || Math.Abs(session.Volume - pair.Value) > 0.02f)
            {
                _restoringSessions.Add(pair.Key);
                FadeTo(session, pair.Value, removeOriginalWhenDone: true);
            }
        }

        _ducking = false;
    }

    private void FadeTo(AudioSessionHandle session, float targetVolume, bool removeOriginalWhenDone = false)
    {
        CancelFade(session.Id);

        if (!Config.UseFade || Config.FadeDurationMs <= 0)
        {
            session.SetVolume(targetVolume);
            if (removeOriginalWhenDone)
            {
                RememberNormalVolume(session, targetVolume);
                _originalVolumes.Remove(session.Id);
                _restoringSessions.Remove(session.Id);
            }
            return;
        }

        var sourceVolume = session.Volume;
        var duration = Math.Max(Config.FadeStepMs, Config.FadeDurationMs);
        var steps = Math.Max(1, duration / Math.Max(10, Config.FadeStepMs));
        var cts = new CancellationTokenSource();
        _fadeJobs[session.Id] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                for (var step = 1; step <= steps; step++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    var next = sourceVolume + ((targetVolume - sourceVolume) * step / steps);
                    session.SetVolume(next);
                    await Task.Delay(Config.FadeStepMs, cts.Token);
                }

                session.SetVolume(targetVolume);
                if (removeOriginalWhenDone)
                {
                    RememberNormalVolume(session, targetVolume);
                    _originalVolumes.Remove(session.Id);
                    _restoringSessions.Remove(session.Id);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                if (removeOriginalWhenDone)
                {
                    _restoringSessions.Remove(session.Id);
                }
            }
            finally
            {
                if (_fadeJobs.TryGetValue(session.Id, out var current) && ReferenceEquals(current, cts))
                {
                    _fadeJobs.Remove(session.Id);
                }
                cts.Dispose();
            }
        });
    }

    private bool IsMicrophoneActive()
    {
        if (!Config.DuckOnMicrophone || _captureDevice is null)
        {
            return false;
        }

        try
        {
            return _captureDevice.AudioMeterInformation.MasterPeakValue >= Config.MicrophoneThreshold;
        }
        catch
        {
            _captureDevice.Dispose();
            _captureDevice = null;
            return false;
        }
    }

    private float ResolveOriginalVolume(AudioSessionHandle target)
    {
        var currentVolume = target.Volume;
        var processName = NormalizeProcessName(target.ProcessName);

        if (currentVolume > Config.DuckVolume + 0.05f)
        {
            RememberNormalVolume(target, currentVolume);
            return currentVolume;
        }

        if (Config.NormalMusicVolumes.TryGetValue(processName, out var rememberedVolume) && rememberedVolume > Config.DuckVolume + 0.05f)
        {
            return Math.Clamp(rememberedVolume, 0f, 1f);
        }

        return currentVolume;
    }

    private void RememberNormalVolume(AudioSessionHandle session, float volume)
    {
        if (volume <= Config.DuckVolume + 0.05f)
        {
            return;
        }

        var processName = NormalizeProcessName(session.ProcessName);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        Config.NormalMusicVolumes[processName] = Math.Clamp(volume, 0f, 1f);
        Config.Save();
    }

    private void HealRememberedMusicVolumes()
    {
        foreach (var session in _sessions.Values.ToList())
        {
            var processName = NormalizeProcessName(session.ProcessName);
            if (!CanWriteSessionVolume(session) || !Config.NormalMusicVolumes.TryGetValue(processName, out var rememberedVolume))
            {
                continue;
            }

            if (rememberedVolume > Config.DuckVolume + 0.05f && session.Volume <= Config.DuckVolume + 0.03f)
            {
                session.SetVolume(Math.Clamp(rememberedVolume, 0f, 1f));
            }
        }
    }

    private void ResetDevices()
    {
        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }

        _sessions.Clear();
        _originalVolumes.Clear();
        _restoringSessions.Clear();
        _renderDevice?.Dispose();
        _captureDevice?.Dispose();
        _enumerator?.Dispose();
        _renderDevice = null;
        _captureDevice = null;
        _enumerator = null;
        _ducking = false;
    }

    private DuckingState EmptyState() => new(
        Config.Enabled,
        false,
        false,
        "Starting",
        "Starting",
        [],
        []);

    private static string LabelFor(AudioSessionHandle session)
    {
        var name = NormalizeProcessName(session.ProcessName);
        return string.IsNullOrWhiteSpace(session.DisplayName) ? name : $"{name} - {session.DisplayName}";
    }

    private bool IsConfiguredMusicTarget(AudioSessionHandle session)
    {
        var processName = NormalizeProcessName(session.ProcessName);
        return Config.MusicProcesses.Any(target => ProcessMatches(processName, target));
    }

    private bool IsIgnoredProcess(AudioSessionHandle session)
    {
        var processName = NormalizeProcessName(session.ProcessName);
        return Config.IgnoredTriggerProcesses.Any(target => ProcessMatches(processName, target));
    }

    private bool CanWriteSessionVolume(AudioSessionHandle session)
    {
        return !session.IsSystemSounds
            && !session.IsExpired
            && !IsIgnoredProcess(session)
            && IsConfiguredMusicTarget(session);
    }

    internal static bool ProcessMatches(string processName, string pattern)
    {
        var normalizedPattern = NormalizeProcessName(pattern);
        return string.Equals(processName, normalizedPattern, StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeProcessName(string value)
    {
        var name = Path.GetFileNameWithoutExtension(value.Trim());
        return string.IsNullOrWhiteSpace(name) ? value.Trim() : name;
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

    private void CancelFade(string sessionId)
    {
        if (!_fadeJobs.TryGetValue(sessionId, out var cts))
        {
            return;
        }

        cts.Cancel();
        _fadeJobs.Remove(sessionId);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
        RestoreTargets(force: true);
        foreach (var cts in _fadeJobs.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        ResetDevices();
    }
}
