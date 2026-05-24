using System.Diagnostics;

namespace AdaptiveMusic.UI;

public sealed class ProcessPickerForm : Form
{
    private readonly TextBox _searchBox = new();
    private readonly ListView _processes = new();
    private readonly Button _addButton = new();
    private readonly Button _cancelButton = new();
    private readonly List<ProcessItem> _allItems;

    public ProcessPickerForm(IEnumerable<string> existingMusicProcesses, IEnumerable<string> mixerProcesses)
    {
        ExistingMusicProcesses = existingMusicProcesses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        MixerProcesses = mixerProcesses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _allItems = LoadProcesses(MixerProcesses);

        Text = "添加音乐程序";
        StartPosition = FormStartPosition.CenterParent;
        Width = 760;
        Height = 520;
        MinimumSize = new Size(620, 420);

        _searchBox.Dock = DockStyle.Top;
        _searchBox.PlaceholderText = "搜索正在运行的程序...";
        _searchBox.Margin = new Padding(8);
        _searchBox.TextChanged += (_, _) => RefreshList();

        _processes.Dock = DockStyle.Fill;
        _processes.View = View.Details;
        _processes.FullRowSelect = true;
        _processes.MultiSelect = true;
        _processes.Columns.Add("来源", 90);
        _processes.Columns.Add("进程", 180);
        _processes.Columns.Add("PID", 80);
        _processes.Columns.Add("窗口标题", 380);
        _processes.DoubleClick += (_, _) => AddSelected();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };

        _addButton.Text = "添加";
        _addButton.Width = 96;
        _addButton.Click += (_, _) => AddSelected();

        _cancelButton.Text = "取消";
        _cancelButton.Width = 96;
        _cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;

        footer.Controls.Add(_addButton);
        footer.Controls.Add(_cancelButton);

        Controls.Add(_processes);
        Controls.Add(footer);
        Controls.Add(_searchBox);

        RefreshList();
    }

    private HashSet<string> ExistingMusicProcesses { get; }
    private HashSet<string> MixerProcesses { get; }
    public IReadOnlyList<string> SelectedProcessNames { get; private set; } = [];

    private void RefreshList()
    {
        var query = _searchBox.Text.Trim();
        var items = string.IsNullOrWhiteSpace(query)
            ? _allItems
            : _allItems
                .Where(item =>
                    item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.WindowTitle.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        _processes.BeginUpdate();
        _processes.Items.Clear();
        foreach (var process in items)
        {
            var item = new ListViewItem(process.FromMixer ? "音量合成器" : "运行进程");
            item.SubItems.Add(process.Name);
            item.SubItems.Add(process.Id.ToString());
            item.SubItems.Add(process.WindowTitle);
            item.Tag = process;
            if (ExistingMusicProcesses.Contains(process.Name))
            {
                item.ForeColor = SystemColors.GrayText;
            }
            _processes.Items.Add(item);
        }
        _processes.EndUpdate();
    }

    private void AddSelected()
    {
        SelectedProcessNames = _processes.SelectedItems
            .Cast<ListViewItem>()
            .Select(item => (ProcessItem)item.Tag!)
            .Select(item => item.Name)
            .Where(name => !ExistingMusicProcesses.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        DialogResult = DialogResult.OK;
    }

    private static List<ProcessItem> LoadProcesses(IReadOnlySet<string> mixerProcesses)
    {
        return Process.GetProcesses()
            .Select(process =>
            {
                try
                {
                    var name = process.ProcessName;
                    return new ProcessItem(
                        name,
                        process.Id,
                        process.MainWindowTitle ?? string.Empty,
                        mixerProcesses.Contains(name));
                }
                catch
                {
                    return null;
                }
            })
            .Where(item => item is not null)
            .Cast<ProcessItem>()
            .GroupBy(item => $"{item.Name}:{item.Id}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(item => item.FromMixer)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToList();
    }

    private sealed record ProcessItem(string Name, int Id, string WindowTitle, bool FromMixer);
}
