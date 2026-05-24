using AdaptiveMusic.Models;

namespace AdaptiveMusic.UI;

public sealed class StatusForm : Form
{
    private readonly Label _summary = new();
    private readonly ListView _sessions = new();
    private readonly TextBox _triggers = new();

    public StatusForm()
    {
        Text = "Adaptive Music";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 980;
        Height = 620;
        MinimumSize = new Size(820, 460);

        _summary.Dock = DockStyle.Top;
        _summary.Height = 72;
        _summary.Padding = new Padding(12);
        _summary.Font = new Font(Font.FontFamily, 10.5f);

        _triggers.Dock = DockStyle.Bottom;
        _triggers.Height = 86;
        _triggers.Multiline = true;
        _triggers.ReadOnly = true;
        _triggers.ScrollBars = ScrollBars.Vertical;

        _sessions.Dock = DockStyle.Fill;
        _sessions.View = View.Details;
        _sessions.FullRowSelect = true;
        _sessions.GridLines = true;
        _sessions.Columns.Add("Role", 90);
        _sessions.Columns.Add("Process", 170);
        _sessions.Columns.Add("PID", 70);
        _sessions.Columns.Add("Peak", 80);
        _sessions.Columns.Add("Volume", 80);
        _sessions.Columns.Add("Muted", 70);
        _sessions.Columns.Add("Display", 380);

        Controls.Add(_sessions);
        Controls.Add(_triggers);
        Controls.Add(_summary);
    }

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

    public void UpdateState(DuckingState state)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateState(state));
            return;
        }

        _summary.Text =
            $"Enabled: {state.Enabled}    Ducking: {state.Ducking}    Microphone: {state.MicrophoneActive}\r\n" +
            $"Output: {state.RenderDeviceName}\r\nInput: {state.CaptureDeviceName}";

        _triggers.Text = state.ActiveTriggers.Count == 0
            ? "Active triggers: none"
            : "Active triggers:\r\n" + string.Join("\r\n", state.ActiveTriggers);

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
            _sessions.Items.Add(item);
        }
        _sessions.EndUpdate();
    }
}
