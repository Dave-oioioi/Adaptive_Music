# Adaptive Music

Adaptive Music is a Windows tray app that lowers selected music apps when other audio or microphone activity is detected, then restores the original per-app volume after the trigger stops.

## MVP features

- Runs as a Windows tray app.
- Uses Windows Core Audio through NAudio.
- Detects non-music playback sessions by peak level.
- Optionally detects microphone input activity.
- Ducks only configured music processes, not the system master volume.
- Restores each music session to the volume captured before ducking.
- Provides a live status window showing sessions, peaks, volume, and roles.
- Stores settings in `%APPDATA%\AdaptiveMusic\config.json`.

## Default config

```json
{
  "enabled": true,
  "musicProcesses": ["Spotify", "cloudmusic", "foobar2000", "MusicBee", "AIMP"],
  "ignoredTriggerProcesses": ["AdaptiveMusic"],
  "duckVolume": 0.25,
  "triggerThreshold": 0.015,
  "microphoneThreshold": 0.02,
  "restoreDelayMs": 1500,
  "pollIntervalMs": 150,
  "fadeStepMs": 35,
  "fadeDurationMs": 280,
  "duckOnMicrophone": true
}
```

Process names are matched without `.exe`.

## Project structure

- `Core/` audio session monitoring and ducking engine.
- `Configuration/` JSON config loading and saving.
- `Models/` immutable status snapshots.
- `UI/` tray app and live status window.
- `Docs/` design notes and operational docs.
- `Assets/` icons and release images.

## Development

```powershell
dotnet restore
dotnet build
dotnet run
```

Double-click the tray icon to open the live status window.
