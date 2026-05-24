namespace AdaptiveMusic.UI;

public sealed class PercentInputForm : Form
{
    private readonly NumericUpDown _input = new();

    public PercentInputForm(float currentValue)
    {
        Text = "Duck Volume";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 320;
        Height = 150;

        var label = new Label
        {
            Text = "Lower music to this volume:",
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(12, 10, 12, 0)
        };

        _input.Minimum = 1;
        _input.Maximum = 100;
        _input.Value = Math.Clamp((decimal)(currentValue * 100f), 1m, 100m);
        _input.Dock = DockStyle.Top;
        _input.Margin = new Padding(12);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };

        var ok = new Button { Text = "Save", Width = 88 };
        ok.Click += (_, _) => DialogResult = DialogResult.OK;

        var cancel = new Button { Text = "Cancel", Width = 88 };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        footer.Controls.Add(ok);
        footer.Controls.Add(cancel);

        Controls.Add(footer);
        Controls.Add(_input);
        Controls.Add(label);
    }

    public float SelectedValue => (float)_input.Value / 100f;
}
