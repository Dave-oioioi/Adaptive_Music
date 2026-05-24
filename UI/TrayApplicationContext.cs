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
        _service = new AdaptiveMusicService(_config);
        _service.StateChanged += ServiceOnStateChanged;

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Adaptive Music",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        _notifyIcon.DoubleClick += (_, _) => ShowStatus();

        _service.Start();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var enabled = new ToolStripMenuItem("Enabled")
        {
            Checked = _config.Enabled,
            CheckOnClick = true
        };
        enabled.CheckedChanged += (_, _) =>
        {
            _config.Enabled = enabled.Checked;
            _config.Save();
            _service.ReloadConfig(_config);
        };

        var status = new ToolStripMenuItem("Show Status", null, (_, _) => ShowStatus());
        var scanAudible = new ToolStripMenuItem("Scan Audible Apps as Music", null, (_, _) => ScanAudibleApps());
        var addProcess = new ToolStripMenuItem("Add Music Process...", null, (_, _) => AddMusicProcess());
        var duckVolume = new ToolStripMenuItem($"Duck Volume: {_config.DuckVolume:P0}", null, (_, _) => SetDuckVolume());
        var openConfig = new ToolStripMenuItem("Open Config JSON", null, (_, _) => _config.OpenInEditor());
        var reload = new ToolStripMenuItem("Reload Config", null, (_, _) => ReloadConfig());
        var quit = new ToolStripMenuItem("Quit", null, (_, _) => ExitThread());

        menu.Items.Add(enabled);
        menu.Items.Add(status);
        menu.Items.Add(scanAudible);
        menu.Items.Add(addProcess);
        menu.Items.Add(duckVolume);
        menu.Items.Add(openConfig);
        menu.Items.Add(reload);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(quit);
        return menu;
    }

    private void ShowStatus()
    {
        _statusForm.Show();
        _statusForm.WindowState = FormWindowState.Normal;
        _statusForm.Activate();
        _statusForm.UpdateState(_service.State);
    }

    private void ScanAudibleApps()
    {
        var audible = _service.GetAudibleProcessNames()
            .Where(name => !_config.MusicProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (audible.Count == 0)
        {
            MessageBox.Show("No new audible apps found. Start music playback, then scan again.", "Adaptive Music");
            return;
        }

        AddMusicProcesses(audible);
        MessageBox.Show("Added music apps:\r\n" + string.Join("\r\n", audible), "Adaptive Music");
    }

    private void AddMusicProcess()
    {
        using var picker = new ProcessPickerForm(_config.MusicProcesses);
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
    }

    private void SetDuckVolume()
    {
        using var dialog = new PercentInputForm(_config.DuckVolume);
        if (dialog.ShowDialog(_statusForm.Visible ? _statusForm : null) != DialogResult.OK)
        {
            return;
        }

        _config.DuckVolume = dialog.SelectedValue;
        _config.Save();
        _service.ReloadConfig(_config);
        _notifyIcon.ContextMenuStrip = BuildMenu();
    }

    private void ReloadConfig()
    {
        _config = AppConfig.LoadOrCreate();
        _service.ReloadConfig(_config);
        _notifyIcon.ContextMenuStrip = BuildMenu();
    }

    private void ServiceOnStateChanged(object? sender, DuckingState state)
    {
        _statusForm.UpdateState(state);
        _notifyIcon.Text = state.Ducking ? "Adaptive Music - ducking" : "Adaptive Music";
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
