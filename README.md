# Adaptive Music — 自动压低音乐

[![Release](https://img.shields.io/badge/Release-v1.0.0-%23D27800)](https://github.com/daweihou/AdaptiveMusic/releases/tag/Latest-Version)
[![License](https://img.shields.io/badge/License-MIT-green)](./LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-lightgrey)]()

[daweihou/AdaptiveMusic](https://github.com/daweihou/AdaptiveMusic)

---

## 功能概述

1. **自动压低音乐** — 检测到其他音频活动、麦克风输入或键盘输入时，自动降低音乐播放器音量
2. **进程级精准控制** — 只修改标记为"音乐应用"的进程音量，不影响系统主音量和触发源
3. **智能恢复** — 触发停止后，通过淡入淡出平滑恢复每个音乐应用的原始音量
4. **实时监控面板** — 控制台窗口实时显示所有音频会话的进程名、峰值、音量和角色
5. **一键扫描添加** — 自动发现当前发声应用并加入音乐列表，也支持手动搜索运行中的进程

---

## 快速安装

### 方式一：下载安装包（推荐）

下载 [AdaptiveMusic-Setup-v1.0.0.exe](https://github.com/daweihou/AdaptiveMusic/releases/download/Latest-Version/AdaptiveMusic-Setup-v1.0.0.exe)，双击运行即可。

- 安装到 `%APPDATA%\.codex\skills\adaptive-music\scripts`
- 配置自动保存到 `%APPDATA%\AdaptiveMusic\config.json`
- 开机自启可在控制台"设置"页一键开启

### 方式二：源码构建

```powershell
# 需要 .NET 9.0 SDK
git clone https://github.com/daweihou/AdaptiveMusic.git
cd AdaptiveMusic
dotnet restore
dotnet build
dotnet run
```

---

## 仓库目录

| 目录 | 说明 |
|------|------|
| `Core/` | 音频会话监听、键盘钩子、压低/恢复引擎 |
| `Configuration/` | JSON 配置文件加载与保存 |
| `Models/` | 不可变状态快照（DuckingState / AudioSessionSnapshot） |
| `UI/` | 系统托盘、控制台窗口、主题系统 |
| `Tests/` | xUnit 单元测试（21 个用例） |
| `Docs/` | 设计笔记与参考文档 |
| `Assets/` | 图标与发布素材 |

---

## 技术栈

- **音频引擎**: NAudio 2.2（Windows Core Audio API）
- **键盘检测**: SetWindowsHookEx (WH_KEYBOARD_LL) 全局钩子
- **配置管理**: System.Text.Json（含注释和尾随逗号容忍）
- **UI 框架**: .NET 9.0 Windows Forms
- **测试框架**: xUnit

---

## 使用方法

1. 启动后 AM 驻留在系统托盘区
2. 双击托盘图标或右键 → "打开控制台" 进入主界面
3. 播放音乐，切到"应用"页点击 **扫描正在发声的音乐** 自动添加播放器
4. 其他程序发声、麦克风激活或敲击键盘时，音乐音量自动降低
5. 触发停止后，音量淡入恢复

### 控制台功能

| 页面 | 功能 |
|------|------|
| **概览** | 当前监听/压低状态、音频设备、触发源列表、快速上手引导 |
| **应用** | 管理会被降低音量的音乐程序（扫描/添加/移除） |
| **会话** | 实时显示所有音频会话的进程、峰值、音量、角色 |
| **设置** | 压低音量、淡入淡出、麦克风/键盘触发、开机自启、外观主题 |

---

## 默认配置

```json
{
  "enabled": true,
  "musicProcesses": ["Spotify", "cloudmusic", "QQMusic", "foobar2000", "MusicBee", "AIMP"],
  "duckVolume": 0.10,
  "triggerThreshold": 0.015,
  "microphoneThreshold": 0.02,
  "restoreDelayMs": 1500,
  "pollIntervalMs": 150,
  "useFade": true,
  "fadeDurationMs": 280,
  "duckOnMicrophone": true,
  "duckOnTyping": true,
  "themeMode": "System"
}
```

配置文件位于 `%APPDATA%\AdaptiveMusic\config.json`，支持直接编辑或通过控制台"设置"页修改。

---

## License

MIT © [daweihou](https://github.com/daweihou)
