using AdaptiveMusic.Configuration;
using AdaptiveMusic.Models;

namespace AdaptiveMusic.UI;

public sealed class StatusForm : Form
{
    private readonly CheckBox _enabled = new();
    private readonly CheckBox _duckOnTyping = new();
    private readonly CheckBox _duckOnMicrophone = new();
    private readonly CheckBox _startWithWindows = new();
    private readonly Label _status = new();
    private readonly Label _devices = new();
    private readonly TrackBar _duckVolume = new();
    private readonly Label _duckVolumeValue = new();
    private readonly CheckBox _useFade = new();
    private readonly TrackBar _fadeDuration = new();
    private readonly Label _fadeDurationValue = new();
    private readonly ComboBox _themeMode = new();
    private readonly ListBox _musicTargets = new();
    private readonly ListView _sessions = new();
    private readonly ListBox _triggers = new();
    private AppTheme _theme = AppThemes.Dark;
    private bool _updating;

    public StatusForm()
    {
        Text = "自适应音乐";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(1020, 620);
        BackColor = _theme.PageBackground;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(18),
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = BuildHeader();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);
        root.Controls.Add(BuildSidebar(), 0, 1);
        root.Controls.Add(BuildSessionsPanel(), 1, 1);

        Controls.Add(root);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
    }

    public event EventHandler<bool>? EnabledChangedByUser;
    public event EventHandler<bool>? DuckOnTypingChangedByUser;
    public event EventHandler<bool>? DuckOnMicrophoneChangedByUser;
    public event EventHandler<bool>? StartWithWindowsChangedByUser;
    public event EventHandler<float>? DuckVolumeChangedByUser;
    public event EventHandler<bool>? UseFadeChangedByUser;
    public event EventHandler<int>? FadeDurationChangedByUser;
    public event EventHandler<string>? ThemeModeChangedByUser;
    public event EventHandler? ScanAudibleRequested;
    public event EventHandler? AddProcessRequested;
    public event EventHandler<string>? RemoveMusicTargetRequested;
    public event EventHandler? OpenConfigRequested;
    public event EventHandler? ReloadConfigRequested;
    public event EventHandler? ResetDefaultsRequested;

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

    public void ApplyConfig(AppConfig config, bool startWithWindows)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyConfig(config, startWithWindows));
            return;
        }

        _updating = true;
        _theme = AppThemes.Resolve(config);
        ApplyTheme();
        _enabled.Checked = config.Enabled;
        _duckOnTyping.Checked = config.DuckOnTyping;
        _duckOnMicrophone.Checked = config.DuckOnMicrophone;
        _startWithWindows.Checked = startWithWindows;
        _duckVolume.Value = Math.Clamp((int)Math.Round(config.DuckVolume * 100), _duckVolume.Minimum, _duckVolume.Maximum);
        _duckVolumeValue.Text = $"{_duckVolume.Value}%";
        _useFade.Checked = config.UseFade;
        _fadeDuration.Value = Math.Clamp(config.FadeDurationMs, _fadeDuration.Minimum, _fadeDuration.Maximum);
        _fadeDurationValue.Text = $"{_fadeDuration.Value} ms";
        _themeMode.SelectedItem = ToThemeLabel(config.ThemeMode);
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
            ? "正在压低音乐"
            : state.Enabled
                ? "正在监听"
                : "已暂停";
        _status.ForeColor = state.Ducking ? Color.FromArgb(255, 159, 10) : state.Enabled ? Color.FromArgb(48, 209, 88) : _theme.SecondaryText;
        _devices.Text = $"输出设备：{state.RenderDeviceName}\r\n输入设备：{state.CaptureDeviceName}    麦克风：{(state.MicrophoneActive ? "有输入" : "空闲")}";

        _triggers.Items.Clear();
        if (state.ActiveTriggers.Count == 0)
        {
            _triggers.Items.Add("暂无触发源");
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
            var role = session.IsMusicTarget ? "音乐" : session.IsTrigger ? "触发源" : session.IsSystemSounds ? "系统" : "";
            var item = new ListViewItem(role);
            item.SubItems.Add(session.ProcessName);
            item.SubItems.Add(session.ProcessId.ToString());
            item.SubItems.Add(session.Peak.ToString("0.000"));
            item.SubItems.Add($"{session.Volume:P0}");
            item.SubItems.Add(session.Muted ? "是" : "否");
            item.SubItems.Add(session.DisplayName);
            item.BackColor = session.IsMusicTarget ? _theme.MusicRow : session.IsTrigger ? _theme.TriggerRow : _theme.GroupBackground;
            item.ForeColor = _theme.PrimaryText;
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
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, BackColor = BackColor };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "自适应音乐",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 18f, FontStyle.Bold),
            ForeColor = _theme.PrimaryText
        };
        _status.Dock = DockStyle.Fill;
        _status.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
        _devices.Dock = DockStyle.Fill;
        _devices.ForeColor = _theme.SecondaryText;
        left.Controls.Add(title, 0, 0);
        left.Controls.Add(_status, 0, 1);
        left.Controls.Add(_devices, 0, 2);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = BackColor
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        _enabled.Text = "启用";
        _enabled.Dock = DockStyle.Fill;
        _enabled.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                EnabledChangedByUser?.Invoke(this, _enabled.Checked);
            }
        };
        _duckOnTyping.Text = "打字键入时降低";
        _duckOnTyping.Dock = DockStyle.Fill;
        _duckOnTyping.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                DuckOnTypingChangedByUser?.Invoke(this, _duckOnTyping.Checked);
            }
        };
        _duckOnMicrophone.Text = "麦克风输入时降低";
        _duckOnMicrophone.Dock = DockStyle.Fill;
        _duckOnMicrophone.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                DuckOnMicrophoneChangedByUser?.Invoke(this, _duckOnMicrophone.Checked);
            }
        };
        right.Controls.Add(_enabled, 0, 0);
        right.Controls.Add(_duckOnTyping, 1, 0);
        right.Controls.Add(_duckOnMicrophone, 2, 0);

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
            Padding = new Padding(0, 0, 14, 0),
            BackColor = BackColor
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 164));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 44));

        panel.Controls.Add(BuildDuckVolumePanel(), 0, 0);
        panel.Controls.Add(BuildMusicActions(), 0, 1);
        panel.Controls.Add(_musicTargets, 0, 2);
        panel.Controls.Add(BuildTriggerHeader(), 0, 3);
        panel.Controls.Add(_triggers, 0, 4);

        _musicTargets.Dock = DockStyle.Fill;
        _musicTargets.BorderStyle = BorderStyle.None;
        _musicTargets.BackColor = _theme.GroupBackground;
        _musicTargets.ForeColor = _theme.PrimaryText;
        _triggers.Dock = DockStyle.Fill;
        _triggers.BorderStyle = BorderStyle.None;
        _triggers.BackColor = _theme.GroupBackground;
        _triggers.ForeColor = _theme.PrimaryText;
        return panel;
    }

    private Control BuildDuckVolumePanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, BackColor = _theme.GroupBackground, Padding = new Padding(14, 10, 14, 8) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var label = new Label
        {
            Text = "压低后音量",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            ForeColor = _theme.PrimaryText
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
        _duckVolumeValue.TextAlign = ContentAlignment.MiddleLeft;
        _duckVolumeValue.ForeColor = _theme.SecondaryText;

        _useFade.Text = "使用音量渐变";
        _useFade.Dock = DockStyle.Fill;
        _useFade.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                UseFadeChangedByUser?.Invoke(this, _useFade.Checked);
            }
        };

        _fadeDuration.Minimum = 0;
        _fadeDuration.Maximum = 3000;
        _fadeDuration.TickFrequency = 500;
        _fadeDuration.SmallChange = 50;
        _fadeDuration.LargeChange = 250;
        _fadeDuration.Dock = DockStyle.Fill;
        _fadeDuration.Scroll += (_, _) => _fadeDurationValue.Text = $"{_fadeDuration.Value} ms";
        _fadeDuration.ValueChanged += (_, _) =>
        {
            if (!_updating)
            {
                _fadeDurationValue.Text = $"{_fadeDuration.Value} ms";
                FadeDurationChangedByUser?.Invoke(this, _fadeDuration.Value);
            }
        };

        _fadeDurationValue.Dock = DockStyle.Fill;
        _fadeDurationValue.TextAlign = ContentAlignment.MiddleLeft;
        _fadeDurationValue.ForeColor = _theme.SecondaryText;

        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(_duckVolume, 0, 1);
        panel.Controls.Add(_useFade, 0, 2);
        panel.Controls.Add(_fadeDuration, 0, 3);
        panel.Controls.Add(_fadeDurationValue, 0, 4);
        panel.Controls.Add(BuildSettingsRow(), 0, 5);
        return panel;
    }

    private Control BuildSettingsRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = _theme.GroupBackground
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        _duckVolumeValue.Dock = DockStyle.Fill;
        _duckVolumeValue.TextAlign = ContentAlignment.MiddleLeft;

        _themeMode.Dock = DockStyle.Fill;
        _themeMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeMode.Items.AddRange(["跟随系统", "浅色", "深色"]);
        _themeMode.SelectedIndexChanged += (_, _) =>
        {
            if (!_updating && _themeMode.SelectedItem is string selected)
            {
                ThemeModeChangedByUser?.Invoke(this, FromThemeLabel(selected));
            }
        };

        row.Controls.Add(_duckVolumeValue, 0, 0);
        row.Controls.Add(_themeMode, 1, 0);
        return row;
    }

    private Control BuildMusicActions()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _theme.PageBackground,
            Padding = new Padding(0, 10, 0, 8)
        };
        panel.ColumnCount = 2;
        panel.RowCount = 3;
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var label = new Label
        {
            Text = "会被降低音量的应用",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            ForeColor = _theme.PrimaryText
        };

        var scan = CreateActionButton("扫描发声");
        scan.Click += (_, _) => ScanAudibleRequested?.Invoke(this, EventArgs.Empty);
        var add = CreateActionButton("手动添加");
        add.Click += (_, _) => AddProcessRequested?.Invoke(this, EventArgs.Empty);
        var remove = CreateActionButton("移除选中");
        remove.Click += (_, _) =>
        {
            if (_musicTargets.SelectedItem is string process)
            {
                RemoveMusicTargetRequested?.Invoke(this, process);
            }
        };
        panel.Controls.Add(label, 0, 0);
        panel.SetColumnSpan(label, 2);
        panel.Controls.Add(scan, 0, 1);
        panel.Controls.Add(add, 1, 1);
        panel.Controls.Add(remove, 0, 2);
        panel.SetColumnSpan(remove, 2);
        return panel;
    }

    private Control BuildTriggerHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _theme.PageBackground,
            Padding = new Padding(0, 10, 0, 8)
        };
        panel.ColumnCount = 2;
        panel.RowCount = 2;
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var label = new Label
        {
            Text = "其他声音触发源",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            ForeColor = _theme.PrimaryText
        };
        _startWithWindows.Text = "开机自启";
        _startWithWindows.Dock = DockStyle.Fill;
        _startWithWindows.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                StartWithWindowsChangedByUser?.Invoke(this, _startWithWindows.Checked);
            }
        };

        var config = CreateActionButton("打开配置");
        config.Click += (_, _) => OpenConfigRequested?.Invoke(this, EventArgs.Empty);
        var reload = CreateActionButton("重载配置");
        reload.Click += (_, _) => ReloadConfigRequested?.Invoke(this, EventArgs.Empty);
        var reset = CreateActionButton("恢复默认");
        reset.Click += (_, _) => ResetDefaultsRequested?.Invoke(this, EventArgs.Empty);
        panel.Controls.Add(label, 0, 0);
        panel.SetColumnSpan(label, 2);
        panel.Controls.Add(_startWithWindows, 0, 1);
        panel.SetColumnSpan(_startWithWindows, 2);
        panel.Controls.Add(config, 0, 2);
        panel.Controls.Add(reload, 1, 2);
        panel.Controls.Add(reset, 0, 3);
        panel.SetColumnSpan(reset, 2);
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
            Text = "全部音频会话",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            ForeColor = _theme.PrimaryText
        };
        _sessions.Dock = DockStyle.Fill;
        _sessions.View = View.Details;
        _sessions.FullRowSelect = true;
        _sessions.GridLines = false;
        _sessions.BorderStyle = BorderStyle.None;
        _sessions.BackColor = _theme.GroupBackground;
        _sessions.ForeColor = _theme.PrimaryText;
        _sessions.Columns.Add("角色", 90);
        _sessions.Columns.Add("进程", 170);
        _sessions.Columns.Add("PID", 70);
        _sessions.Columns.Add("峰值", 80);
        _sessions.Columns.Add("音量", 80);
        _sessions.Columns.Add("静音", 70);
        _sessions.Columns.Add("显示名称", 420);
        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(_sessions, 0, 1);
        return panel;
    }

    private Button CreateActionButton(string text)
    {
        return new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = _theme.GroupBackground,
            ForeColor = _theme.PrimaryBlue,
            FlatAppearance = { BorderColor = Color.FromArgb(72, 72, 74) },
            Margin = new Padding(0, 0, 8, 8)
        };
    }

    private void ApplyTheme()
    {
        BackColor = _theme.PageBackground;
        foreach (Control control in Controls)
        {
            ApplyThemeRecursive(control);
        }

        _musicTargets.BackColor = _theme.GroupBackground;
        _musicTargets.ForeColor = _theme.PrimaryText;
        _triggers.BackColor = _theme.GroupBackground;
        _triggers.ForeColor = _theme.PrimaryText;
        _sessions.BackColor = _theme.GroupBackground;
        _sessions.ForeColor = _theme.PrimaryText;
        _duckVolumeValue.ForeColor = _theme.SecondaryText;
        _fadeDurationValue.ForeColor = _theme.SecondaryText;
        Invalidate(true);
    }

    private void ApplyThemeRecursive(Control control)
    {
        if (control is Button button)
        {
            button.BackColor = _theme.GroupBackground;
            button.ForeColor = _theme.PrimaryBlue;
            button.FlatAppearance.BorderColor = _theme.IsDark ? Color.FromArgb(72, 72, 74) : Color.FromArgb(209, 209, 214);
        }
        else if (control is ListBox or ListView)
        {
            control.BackColor = _theme.GroupBackground;
            control.ForeColor = _theme.PrimaryText;
        }
        else if (control is Label label)
        {
            label.ForeColor = label == _devices || label == _duckVolumeValue ? _theme.SecondaryText : _theme.PrimaryText;
            control.BackColor = Color.Transparent;
        }
        else
        {
            control.BackColor = control is TableLayoutPanel or FlowLayoutPanel ? _theme.PageBackground : _theme.GroupBackground;
            control.ForeColor = _theme.PrimaryText;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeRecursive(child);
        }
    }

    private static string ToThemeLabel(string? mode)
    {
        return mode?.Equals("Light", StringComparison.OrdinalIgnoreCase) == true
            ? "浅色"
            : mode?.Equals("Dark", StringComparison.OrdinalIgnoreCase) == true
                ? "深色"
                : "跟随系统";
    }

    private static string FromThemeLabel(string label)
    {
        return label switch
        {
            "浅色" => "Light",
            "深色" => "Dark",
            _ => "System"
        };
    }
}
