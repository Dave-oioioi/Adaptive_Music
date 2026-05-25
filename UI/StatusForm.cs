using AdaptiveMusic.Configuration;
using AdaptiveMusic.Models;

namespace AdaptiveMusic.UI;

public sealed class StatusForm : Form
{
    private readonly Label _stateTitle = new();
    private readonly Label _stateDetail = new();
    private readonly Label _devices = new();
    private readonly CheckBox _enabled = new();
    private readonly ListBox _musicTargets = new();
    private readonly ListBox _triggers = new();
    private readonly ListView _sessions = new();
    private readonly TrackBar _duckVolume = new();
    private readonly Label _duckVolumeValue = new();
    private readonly CheckBox _useFade = new();
    private readonly TrackBar _fadeDuration = new();
    private readonly Label _fadeDurationValue = new();
    private readonly CheckBox _duckOnTyping = new();
    private readonly CheckBox _duckOnMicrophone = new();
    private readonly CheckBox _startWithWindows = new();
    private readonly ComboBox _themeMode = new();
    private AppTheme _theme = AppThemes.Light;
    private bool _updating;

    public StatusForm()
    {
        Text = "自适应音乐";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1120;
        Height = 820;
        MinimumSize = new Size(980, 700);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHero(), 0, 0);
        root.Controls.Add(BuildTabs(), 0, 1);
        Controls.Add(root);
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
        _duckVolume.Value = Math.Clamp((int)Math.Round(config.DuckVolume * 100), _duckVolume.Minimum, _duckVolume.Maximum);
        _duckVolumeValue.Text = $"{_duckVolume.Value}%";
        _useFade.Checked = config.UseFade;
        _fadeDuration.Value = Math.Clamp(config.FadeDurationMs, _fadeDuration.Minimum, _fadeDuration.Maximum);
        _fadeDurationValue.Text = $"{_fadeDuration.Value} ms";
        _duckOnTyping.Checked = config.DuckOnTyping;
        _duckOnMicrophone.Checked = config.DuckOnMicrophone;
        _startWithWindows.Checked = startWithWindows;
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

        _stateTitle.Text = state.Ducking ? "正在降低音乐音量" : state.Enabled ? "正在监听" : "已暂停";
        _stateTitle.ForeColor = state.Ducking ? Color.FromArgb(191, 90, 0) : state.Enabled ? Color.FromArgb(24, 128, 56) : _theme.SecondaryText;
        _stateDetail.Text = BuildGuidance(state);
        _devices.Text = $"输出：{state.RenderDeviceName}\r\n输入：{state.CaptureDeviceName}    麦克风：{(state.MicrophoneActive ? "有输入" : "空闲")}";

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
            var role = session.IsMusicTarget ? "被降低" : session.IsTrigger ? "触发源" : session.IsSystemSounds ? "系统" : "其他";
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

    private Control BuildHero()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var appTitle = new Label
        {
            Text = "自适应音乐",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 17f, FontStyle.Bold)
        };
        _stateTitle.Dock = DockStyle.Fill;
        _stateTitle.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
        _stateDetail.Dock = DockStyle.Fill;
        _stateDetail.Font = new Font(Font.FontFamily, 9.5f);
        left.Controls.Add(appTitle, 0, 0);
        left.Controls.Add(_stateTitle, 0, 1);
        left.Controls.Add(_stateDetail, 0, 2);

        _enabled.Text = "启用监听";
        _enabled.Dock = DockStyle.Top;
        _enabled.Height = 34;
        _enabled.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                EnabledChangedByUser?.Invoke(this, _enabled.Checked);
            }
        };

        panel.Controls.Add(left, 0, 0);
        panel.Controls.Add(_enabled, 1, 0);
        return panel;
    }

    private Control BuildTabs()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildOverviewTab());
        tabs.TabPages.Add(BuildAppsTab());
        tabs.TabPages.Add(BuildSettingsTab());
        tabs.TabPages.Add(BuildSessionsTab());
        return tabs;
    }

    private TabPage BuildOverviewTab()
    {
        var tab = new TabPage("概览");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(8) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        grid.Controls.Add(CreateGroup("设备状态", _devices), 0, 0);
        grid.Controls.Add(CreateGroup("其他声音触发源", _triggers), 0, 1);
        var quickStart = CreateQuickStart();
        grid.Controls.Add(quickStart, 1, 0);
        grid.SetRowSpan(quickStart, 2);
        tab.Controls.Add(grid);
        return tab;
    }

    private Control CreateQuickStart()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Padding = new Padding(8) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label { Text = "使用流程", Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 11f, FontStyle.Bold) };
        var step1 = new Label { Text = "1. 播放音乐", Dock = DockStyle.Fill };
        var step2 = new Label { Text = "2. 到“应用”页扫描或手动添加音乐播放器", Dock = DockStyle.Fill };
        var step3 = new Label { Text = "3. 其他声音出现时，只会降低已添加应用的音量", Dock = DockStyle.Fill };
        var note = new Label { Text = "提示：不要把浏览器、输入法、游戏误加入左侧列表，除非它就是你的音乐播放器。", Dock = DockStyle.Fill };

        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(step1, 0, 1);
        panel.Controls.Add(step2, 0, 2);
        panel.Controls.Add(step3, 0, 3);
        panel.Controls.Add(note, 0, 4);
        return CreateGroup("快速上手", panel);
    }

    private TabPage BuildAppsTab()
    {
        var tab = new TabPage("应用");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(14) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(0, 0, 6, 6) };
        buttons.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        buttons.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        buttons.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var scan = CreateActionButton("扫描正在发声的音乐");
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
        buttons.Controls.Add(scan, 0, 0);
        buttons.Controls.Add(add, 0, 1);
        buttons.Controls.Add(remove, 0, 2);
        left.Controls.Add(buttons, 0, 0);
        left.Controls.Add(_musicTargets, 0, 1);

        var right = new Label
        {
            Dock = DockStyle.Fill,
            Text = "这里的应用会被降低音量。\r\n\r\n其他应用只会作为触发源，不会被修改音量。\r\n\r\n推荐只加入音乐播放器，例如 QQMusic、Spotify、网易云音乐。",
            Padding = new Padding(8)
        };

        grid.Controls.Add(CreateGroup("会被降低音量的应用", left), 0, 0);
        grid.Controls.Add(CreateGroup("规则说明", right), 1, 0);
        tab.Controls.Add(grid);
        return tab;
    }

    private TabPage BuildSettingsTab()
    {
        var tab = new TabPage("设置");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(14) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        grid.Controls.Add(CreateGroup("音量", BuildVolumeSettings()), 0, 0);
        grid.Controls.Add(CreateGroup("触发与系统", BuildSystemSettings()), 1, 0);
        tab.Controls.Add(grid);
        return tab;
    }

    private Control BuildVolumeSettings()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, Padding = new Padding(8) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label { Text = "压低后音量", Dock = DockStyle.Fill }, 0, 0);
        _duckVolume.Minimum = 1;
        _duckVolume.Maximum = 100;
        _duckVolume.TickFrequency = 10;
        _duckVolume.Dock = DockStyle.Fill;
        _duckVolume.ValueChanged += (_, _) =>
        {
            _duckVolumeValue.Text = $"{_duckVolume.Value}%";
            if (!_updating)
            {
                DuckVolumeChangedByUser?.Invoke(this, _duckVolume.Value / 100f);
            }
        };
        panel.Controls.Add(_duckVolume, 0, 1);
        panel.Controls.Add(_duckVolumeValue, 0, 2);

        _useFade.Text = "使用音量渐变";
        _useFade.Dock = DockStyle.Fill;
        _useFade.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                UseFadeChangedByUser?.Invoke(this, _useFade.Checked);
            }
        };
        panel.Controls.Add(_useFade, 0, 3);

        _fadeDuration.Minimum = 0;
        _fadeDuration.Maximum = 3000;
        _fadeDuration.TickFrequency = 500;
        _fadeDuration.Dock = DockStyle.Fill;
        _fadeDuration.ValueChanged += (_, _) =>
        {
            _fadeDurationValue.Text = $"{_fadeDuration.Value} ms";
            if (!_updating)
            {
                FadeDurationChangedByUser?.Invoke(this, _fadeDuration.Value);
            }
        };
        panel.Controls.Add(_fadeDuration, 0, 4);
        panel.Controls.Add(_fadeDurationValue, 0, 5);
        return panel;
    }

    private Control BuildSystemSettings()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 8, Padding = new Padding(16) };
        for (var i = 0; i < 8; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, i is 5 or 6 or 7 ? 48 : 38));
        }

        _duckOnTyping.Text = "打字键入时降低音乐";
        _duckOnTyping.Dock = DockStyle.Fill;
        _duckOnTyping.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                DuckOnTypingChangedByUser?.Invoke(this, _duckOnTyping.Checked);
            }
        };

        _duckOnMicrophone.Text = "麦克风输入时降低音乐";
        _duckOnMicrophone.Dock = DockStyle.Fill;
        _duckOnMicrophone.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                DuckOnMicrophoneChangedByUser?.Invoke(this, _duckOnMicrophone.Checked);
            }
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

        var openConfig = CreateActionButton("打开配置");
        openConfig.Click += (_, _) => OpenConfigRequested?.Invoke(this, EventArgs.Empty);
        var reload = CreateActionButton("重载配置");
        reload.Click += (_, _) => ReloadConfigRequested?.Invoke(this, EventArgs.Empty);
        var reset = CreateActionButton("恢复默认");
        reset.Click += (_, _) => ResetDefaultsRequested?.Invoke(this, EventArgs.Empty);

        panel.Controls.Add(_duckOnTyping, 0, 0);
        panel.Controls.Add(_duckOnMicrophone, 0, 1);
        panel.Controls.Add(_startWithWindows, 0, 2);
        panel.Controls.Add(new Label { Text = "外观", Dock = DockStyle.Fill }, 0, 3);
        panel.Controls.Add(_themeMode, 0, 4);
        panel.Controls.Add(openConfig, 0, 5);
        panel.Controls.Add(reload, 0, 6);
        panel.Controls.Add(reset, 0, 7);
        return panel;
    }

    private TabPage BuildSessionsTab()
    {
        var tab = new TabPage("会话");
        _sessions.Dock = DockStyle.Fill;
        _sessions.View = View.Details;
        _sessions.FullRowSelect = true;
        _sessions.BorderStyle = BorderStyle.None;
        _sessions.Columns.Add("角色", 90);
        _sessions.Columns.Add("进程", 170);
        _sessions.Columns.Add("PID", 70);
        _sessions.Columns.Add("峰值", 80);
        _sessions.Columns.Add("音量", 80);
        _sessions.Columns.Add("静音", 70);
        _sessions.Columns.Add("显示名称", 420);
        tab.Controls.Add(CreateGroup("全部音频会话", _sessions));
        return tab;
    }

    private Control CreateGroup(string title, Control content)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        content.Dock = DockStyle.Fill;
        group.Controls.Add(content);
        return group;
    }

    private Button CreateActionButton(string text)
    {
        return new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 32,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    private void ApplyTheme()
    {
        BackColor = _theme.PageBackground;
        foreach (Control control in Controls)
        {
            ApplyThemeRecursive(control);
        }
        Invalidate(true);
    }

    private void ApplyThemeRecursive(Control control)
    {
        control.BackColor = control is TextBox or ComboBox ? Color.White : _theme.PageBackground;
        control.ForeColor = _theme.PrimaryText;

        if (control is ListBox or ListView)
        {
            control.BackColor = _theme.GroupBackground;
            control.ForeColor = _theme.PrimaryText;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeRecursive(child);
        }
    }

    private static string BuildGuidance(DuckingState state)
    {
        if (!state.Enabled)
        {
            return "当前已暂停。打开“启用监听”后才会监听其他声音。";
        }

        if (state.Ducking && state.ActiveTriggers.Count > 0)
        {
            return $"触发原因：{string.Join("、", state.ActiveTriggers.Take(2))}。只会降低“应用”页中已添加的程序音量。";
        }

        if (state.Ducking && state.MicrophoneActive)
        {
            return "触发原因：麦克风或输入活动。只会降低“应用”页中已添加的程序音量。";
        }

        var hasMusicTargets = state.Sessions.Any(session => session.IsMusicTarget);
        return hasMusicTargets
            ? "正在等待其他声音。触发后只会降低“应用”页中已添加的程序音量。"
            : "先播放音乐，然后到“应用”页扫描或手动添加音乐播放器。";
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
