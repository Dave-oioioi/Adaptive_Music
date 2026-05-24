using AdaptiveMusic.Configuration;
using AdaptiveMusic.Core;
using AdaptiveMusic.Models;

namespace AdaptiveMusic.UI;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly AdaptiveMusicService _service;
    private readonly StatusForm _statusForm;
    private AppConfig _config;

    public TrayApplicationContext()
    {
        _config = AppConfig.LoadOrCreate();
        _statusForm = new StatusForm();
        WireStatusFormEvents();
        _statusForm.ApplyConfig(_config, StartupManager.IsEnabled());
        _service = new AdaptiveMusicService(_config);
        _service.StateChanged += ServiceOnStateChanged;

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "自适应音乐",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowStatus();
            }
        };
        _notifyIcon.DoubleClick += (_, _) => ShowStatus();

        _service.Start();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        var status = new ToolStripMenuItem("打开控制台", null, (_, _) => ShowStatus());
        var quit = new ToolStripMenuItem("退出", null, (_, _) => ExitThread());

        menu.Items.Add(status);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(quit);
        return menu;
    }

    private void ShowStatus()
    {
        _statusForm.Show();
        _statusForm.WindowState = FormWindowState.Normal;
        _statusForm.Activate();
        _statusForm.ApplyConfig(_config, StartupManager.IsEnabled());
        _statusForm.UpdateState(_service.State);
    }

    private void WireStatusFormEvents()
    {
        _statusForm.EnabledChangedByUser += (_, enabled) =>
        {
            _config.Enabled = enabled;
            SaveAndApplyConfig();
        };
        _statusForm.DuckOnTypingChangedByUser += (_, enabled) =>
        {
            _config.DuckOnTyping = enabled;
            SaveAndApplyConfig(rebuildMenu: false);
        };
        _statusForm.DuckOnMicrophoneChangedByUser += (_, enabled) =>
        {
            _config.DuckOnMicrophone = enabled;
            SaveAndApplyConfig(rebuildMenu: false);
        };
        _statusForm.DuckVolumeChangedByUser += (_, duckVolume) =>
        {
            _config.DuckVolume = duckVolume;
            SaveAndApplyConfig(rebuildMenu: false);
        };
        _statusForm.UseFadeChangedByUser += (_, useFade) =>
        {
            _config.UseFade = useFade;
            SaveAndApplyConfig(rebuildMenu: false);
        };
        _statusForm.FadeDurationChangedByUser += (_, fadeDurationMs) =>
        {
            _config.FadeDurationMs = fadeDurationMs;
            SaveAndApplyConfig(rebuildMenu: false);
        };
        _statusForm.ThemeModeChangedByUser += (_, themeMode) =>
        {
            _config.ThemeMode = themeMode;
            SaveAndApplyConfig(rebuildMenu: false);
        };
        _statusForm.StartWithWindowsChangedByUser += (_, enabled) =>
        {
            StartupManager.SetEnabled(enabled);
            _statusForm.ApplyConfig(_config, StartupManager.IsEnabled());
        };
        _statusForm.ScanAudibleRequested += (_, _) => ScanAudibleApps();
        _statusForm.AddProcessRequested += (_, _) => AddMusicProcess();
        _statusForm.RemoveMusicTargetRequested += (_, process) => RemoveMusicProcess(process);
        _statusForm.OpenConfigRequested += (_, _) => _config.OpenInEditor();
        _statusForm.ReloadConfigRequested += (_, _) => ReloadConfig();
        _statusForm.ResetDefaultsRequested += (_, _) => ResetDefaults();
    }

    private void ScanAudibleApps()
    {
        var audible = _service.GetAudibleProcessNames()
            .Where(name => !_config.MusicProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (audible.Count == 0)
        {
            var mixerProcesses = _service.GetMixerProcessNames()
                .Where(name => !_config.MusicProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (mixerProcesses.Count == 0)
            {
                MessageBox.Show("没有发现新的音频应用。请先让音乐程序出现在系统音量合成器里，然后再扫描。", "自适应音乐");
                return;
            }

            MessageBox.Show("未检测到明显发声峰值。系统音量合成器里有这些应用，请点击“手动添加”选择真正的音乐播放器：\r\n" + string.Join("\r\n", mixerProcesses), "自适应音乐");
            return;
        }

        var confirm = MessageBox.Show(
            "将以下正在发声的应用加入“会被降低音量的应用”列表：\r\n\r\n" +
            string.Join("\r\n", audible) +
            "\r\n\r\n确认这些都是音乐播放器吗？",
            "确认添加音乐应用",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        AddMusicProcesses(audible);
        MessageBox.Show("已添加音乐程序：\r\n" + string.Join("\r\n", audible), "自适应音乐");
    }

    private void AddMusicProcess()
    {
        using var picker = new ProcessPickerForm(_config.MusicProcesses, _service.GetMixerProcessNames());
        if (picker.ShowDialog(_statusForm.Visible ? _statusForm : null) != DialogResult.OK)
        {
            return;
        }

        var selected = picker.SelectedProcessNames.ToList();
        if (selected.Count == 0)
        {
            return;
        }

        AddMusicProcesses(selected);
    }

    private void AddMusicProcesses(IEnumerable<string> processNames)
    {
        foreach (var processName in processNames)
        {
            if (!_config.MusicProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                _config.MusicProcesses.Add(processName);
            }
        }

        _config.Save();
        _service.ReloadConfig(_config);
        _notifyIcon.ContextMenuStrip = BuildMenu();
        _statusForm.ApplyConfig(_config, StartupManager.IsEnabled());
    }

    private void RemoveMusicProcess(string processName)
    {
        _config.MusicProcesses.RemoveAll(process => string.Equals(process, processName, StringComparison.OrdinalIgnoreCase));
        SaveAndApplyConfig();
    }

    private void SetDuckVolume()
    {
        using var dialog = new PercentInputForm(_config.DuckVolume);
        if (dialog.ShowDialog(_statusForm.Visible ? _statusForm : null) != DialogResult.OK)
        {
            return;
        }

        _config.DuckVolume = dialog.SelectedValue;
        SaveAndApplyConfig();
    }

    private void ReloadConfig()
    {
        _config = AppConfig.LoadOrCreate();
        _service.ReloadConfig(_config);
        _notifyIcon.ContextMenuStrip = BuildMenu();
        _statusForm.ApplyConfig(_config, StartupManager.IsEnabled());
    }

    private void ResetDefaults()
    {
        var confirm = MessageBox.Show(
            "确定恢复默认设置吗？音乐应用列表、音量比例、触发选项和外观设置都会恢复默认。",
            "恢复默认设置",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _config = new AppConfig();
        _config.Save();
        StartupManager.SetEnabled(false);
        _service.ReloadConfig(_config);
        _notifyIcon.ContextMenuStrip = BuildMenu();
        _statusForm.ApplyConfig(_config, StartupManager.IsEnabled());
    }

    private void SaveAndApplyConfig(bool rebuildMenu = true)
    {
        _config.Save();
        _service.ReloadConfig(_config);
        if (rebuildMenu)
        {
            _notifyIcon.ContextMenuStrip = BuildMenu();
        }
        _statusForm.ApplyConfig(_config, StartupManager.IsEnabled());
    }

    private void ServiceOnStateChanged(object? sender, DuckingState state)
    {
        _statusForm.UpdateState(state);
        _notifyIcon.Text = state.Ducking ? "自适应音乐 - 正在压低" : "自适应音乐";
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _statusForm.Dispose();
        _service.Dispose();
        base.ExitThreadCore();
    }
}
