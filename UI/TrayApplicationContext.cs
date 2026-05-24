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
        var openConfig = new ToolStripMenuItem("Open Config JSON", null, (_, _) => _config.OpenInEditor());
        var reload = new ToolStripMenuItem("Reload Config", null, (_, _) => ReloadConfig());
        var quit = new ToolStripMenuItem("Quit", null, (_, _) => ExitThread());

        menu.Items.Add(enabled);
        menu.Items.Add(status);
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
