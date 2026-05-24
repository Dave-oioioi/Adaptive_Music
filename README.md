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
- Can scan currently audible apps and add them to the music target list.
- Can search running processes and manually add a music app.
- Can change the ducked music volume percentage from the tray menu.
- Stores settings in `%APPDATA%\AdaptiveMusic\config.json`.

## Default config

```json
{
  "enabled": true,
  "musicProcesses": ["Spotify", "cloudmusic", "QQMusic", "foobar2000", "MusicBee", "AIMP"],
  "ignoredTriggerProcesses": ["AdaptiveMusic"],
  "duckVolume": 0.25,
  "triggerThreshold": 0.015,
  "microphoneThreshold": 0.02,
  "restoreDelayMs": 1500,
  "pollIntervalMs": 150,
  "fadeStepMs": 35,
  "useFade": true,
  "fadeDurationMs": 280,
  "duckOnMicrophone": true,
  "duckOnTyping": true,
  "typingTriggerProcesses": ["TextInputHost"],
  "themeMode": "System",
  "normalMusicVolumes": {}
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

双击托盘图标可以打开控制台窗口。

控制台窗口包含：

- 当前监听/压低状态和正在使用的音频设备。
- 启用/暂停开关。
- 打字键入时降低音乐开关，用于覆盖输入法、键入相关触发。
- 麦克风输入时降低音乐开关，用于覆盖语音输入、通话、录音等麦克风输入。
- 压低后音量滑块。
- 音量渐变开关和渐变时长滑块。
- 开机自启、外观模式、打开/重载配置和恢复默认设置。
- “会被降低音量的应用”列表，以及扫描、添加、移除按钮。
- “其他声音触发源”列表。
- 实时音频会话表，显示进程、峰值、音量和角色。

托盘菜单已简化，只保留：

- `打开控制台`
- `退出`

其他功能都集中在控制台窗口内：

- `扫描发声`：先播放音乐，再扫描，把当前发声应用加入音乐目标。
- `手动添加`：搜索当前运行中的程序，并手动加入音乐目标。
- `移除选中`：从音乐目标列表移除选中的程序。
- `打开配置`：直接编辑高级配置。
- `重载配置`：重新读取配置文件。
