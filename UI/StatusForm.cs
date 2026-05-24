using AdaptiveMusic.Configuration;
using AdaptiveMusic.Models;

namespace AdaptiveMusic.UI;

public sealed class StatusForm : Form
{
    private readonly CheckBox _enabled = new();
    private readonly Label _status = new();
    private readonly Label _devices = new();
    private readonly TrackBar _duckVolume = new();
    private readonly Label _duckVolumeValue = new();
    private readonly ListBox _musicTargets = new();
    private readonly ListView _sessions = new();
    private readonly ListBox _triggers = new();
    private bool _updating;

    public StatusForm()
    {
        Text = "Adaptive Music";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1120;
        Height = 720;
        MinimumSize = new Size(960, 560);
        BackColor = Color.FromArgb(248, 249, 251);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(14),
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = BuildHeader();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);
        root.Controls.Add(BuildSidebar(), 0, 1);
        root.Controls.Add(BuildSessionsPanel(), 1, 1);

        Controls.Add(root);
    }

    public event EventHandler<bool>? EnabledChangedByUser;
    public event EventHandler<float>? DuckVolumeChangedByUser;
    public event EventHandler? ScanAudibleRequested;
    public event EventHandler? AddProcessRequested;
    public event EventHandler<string>? RemoveMusicTargetRequested;
    public event EventHandler? OpenConfigRequested;
    public event EventHandler? ReloadConfigRequested;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    public void ApplyConfig(AppConfig config)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyConfig(config));
            return;
        }

        _updating = true;
        _enabled.Checked = config.Enabled;
        _duckVolume.Value = Math.Clamp((int)Math.Round(config.DuckVolume * 100), _duckVolume.Minimum, _duckVolume.Maximum);
        _duckVolumeValue.Text = $"{_duckVolume.Value}%";
        _musicTargets.Items.Clear();
        foreach (var process in config.MusicProcesses.OrderBy(p => p))
        {
            _musicTargets.Items.Add(process);
        }
        _updating = false;
    }

    public void UpdateState(DuckingState state)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateState(state));
            return;
        }

        _status.Text = state.Ducking
            ? "Ducking music"
            : state.Enabled
                ? "Listening"
                : "Paused";
        _status.ForeColor = state.Ducking ? Color.FromArgb(190, 83, 35) : state.Enabled ? Color.FromArgb(31, 116, 69) : Color.FromArgb(104, 111, 122);
        _devices.Text = $"Output: {state.RenderDeviceName}\r\nInput: {state.CaptureDeviceName}    Mic: {(state.MicrophoneActive ? "active" : "idle")}";

        _triggers.Items.Clear();
        if (state.ActiveTriggers.Count == 0)
        {
            _triggers.Items.Add("No active triggers");
        }
        else
        {
            foreach (var trigger in state.ActiveTriggers)
            {
                _triggers.Items.Add(trigger);
            }
        }

        _sessions.BeginUpdate();
        _sessions.Items.Clear();
        foreach (var session in state.Sessions)
        {
            var role = session.IsMusicTarget ? "Music" : session.IsTrigger ? "Trigger" : session.IsSystemSounds ? "System" : "";
            var item = new ListViewItem(role);
            item.SubItems.Add(session.ProcessName);
            item.SubItems.Add(session.ProcessId.ToString());
            item.SubItems.Add(session.Peak.ToString("0.000"));
            item.SubItems.Add($"{session.Volume:P0}");
            item.SubItems.Add(session.Muted ? "Yes" : "No");
            item.SubItems.Add(session.DisplayName);
            item.BackColor = session.IsMusicTarget ? Color.FromArgb(235, 247, 240) : session.IsTrigger ? Color.FromArgb(255, 244, 232) : Color.White;
            _sessions.Items.Add(item);
        }
        _sessions.EndUpdate();
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BackColor
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, BackColor = BackColor };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "Adaptive Music",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 36, 44)
        };
        _status.Dock = DockStyle.Fill;
        _status.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
        _devices.Dock = DockStyle.Fill;
        _devices.ForeColor = Color.FromArgb(88, 96, 108);
        left.Controls.Add(title, 0, 0);
        left.Controls.Add(_status, 0, 1);
        left.Controls.Add(_devices, 0, 2);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = BackColor
        };
        _enabled.Text = "Enabled";
        _enabled.AutoSize = true;
        _enabled.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                EnabledChangedByUser?.Invoke(this, _enabled.Checked);
            }
        };
        right.Controls.Add(_enabled);

        panel.Controls.Add(left, 0, 0);
        panel.Controls.Add(right, 1, 0);
        return panel;
    }

    private Control BuildSidebar()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            Padding = new Padding(0, 0, 12, 0),
            BackColor = BackColor
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

        panel.Controls.Add(BuildDuckVolumePanel(), 0, 0);
        panel.Controls.Add(BuildMusicToolbar(), 0, 1);
        panel.Controls.Add(_musicTargets, 0, 2);
        panel.Controls.Add(BuildTriggerHeader(), 0, 3);
        panel.Controls.Add(_triggers, 0, 4);

        _musicTargets.Dock = DockStyle.Fill;
        _musicTargets.BorderStyle = BorderStyle.FixedSingle;
        _triggers.Dock = DockStyle.Fill;
        _triggers.BorderStyle = BorderStyle.FixedSingle;
        return panel;
    }

    private Control BuildDuckVolumePanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, BackColor = BackColor };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        var label = new Label
        {
            Text = "Duck volume",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold)
        };

        _duckVolume.Minimum = 1;
        _duckVolume.Maximum = 100;
        _duckVolume.TickFrequency = 10;
        _duckVolume.Dock = DockStyle.Fill;
        _duckVolume.Scroll += (_, _) =>
        {
            _duckVolumeValue.Text = $"{_duckVolume.Value}%";
        };
        _duckVolume.ValueChanged += (_, _) =>
        {
            if (!_updating)
            {
                _duckVolumeValue.Text = $"{_duckVolume.Value}%";
                DuckVolumeChangedByUser?.Invoke(this, _duckVolume.Value / 100f);
            }
        };

        _duckVolumeValue.Dock = DockStyle.Fill;
        _duckVolumeValue.TextAlign = ContentAlignment.MiddleRight;
        _duckVolumeValue.ForeColor = Color.FromArgb(75, 84, 97);

        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(_duckVolume, 0, 1);
        panel.Controls.Add(_duckVolumeValue, 0, 2);
        return panel;
    }

    private Control BuildMusicToolbar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = BackColor
        };
        var scan = CreateSmallButton("Scan");
        scan.Click += (_, _) => ScanAudibleRequested?.Invoke(this, EventArgs.Empty);
        var add = CreateSmallButton("Add");
        add.Click += (_, _) => AddProcessRequested?.Invoke(this, EventArgs.Empty);
        var remove = CreateSmallButton("Remove");
        remove.Click += (_, _) =>
        {
            if (_musicTargets.SelectedItem is string process)
            {
                RemoveMusicTargetRequested?.Invoke(this, process);
            }
        };
        panel.Controls.Add(scan);
        panel.Controls.Add(add);
        panel.Controls.Add(remove);
        return panel;
    }

    private Control BuildTriggerHeader()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = BackColor
        };
        var label = new Label
        {
            Text = "Active triggers",
            Width = 140,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold)
        };
        var config = CreateSmallButton("Config");
        config.Click += (_, _) => OpenConfigRequested?.Invoke(this, EventArgs.Empty);
        var reload = CreateSmallButton("Reload");
        reload.Click += (_, _) => ReloadConfigRequested?.Invoke(this, EventArgs.Empty);
        panel.Controls.Add(label);
        panel.Controls.Add(config);
        panel.Controls.Add(reload);
        return panel;
    }

    private Control BuildSessionsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = BackColor
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var label = new Label
        {
            Text = "Live audio sessions",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold)
        };
        _sessions.Dock = DockStyle.Fill;
        _sessions.View = View.Details;
        _sessions.FullRowSelect = true;
        _sessions.GridLines = false;
        _sessions.BorderStyle = BorderStyle.FixedSingle;
        _sessions.Columns.Add("Role", 90);
        _sessions.Columns.Add("Process", 170);
        _sessions.Columns.Add("PID", 70);
        _sessions.Columns.Add("Peak", 80);
        _sessions.Columns.Add("Volume", 80);
        _sessions.Columns.Add("Muted", 70);
        _sessions.Columns.Add("Display", 420);
        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(_sessions, 0, 1);
        return panel;
    }

    private static Button CreateSmallButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 82,
            Height = 30,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(0, 0, 6, 0)
        };
    }
}
