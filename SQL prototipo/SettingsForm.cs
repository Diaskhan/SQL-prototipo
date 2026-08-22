using SQL_prototipo.Models;

namespace SQL_prototipo;

/// <summary>
/// Modal dialog that lets the user edit application settings.
/// </summary>
public class SettingsForm : Form
{
    private readonly CheckBox _chkShowColumns;

    /// <summary>
    /// The settings edited by the dialog. Reflects the user's choices after the dialog closes with OK.
    /// </summary>
    public AppSettings Settings { get; }

    public SettingsForm(AppSettings settings)
    {
        Settings = settings;

        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 130);

        var groupObjectTree = new GroupBox
        {
            Text = "Object Tree",
            Location = new Point(12, 12),
            Size = new Size(336, 60)
        };

        _chkShowColumns = new CheckBox
        {
            Text = "Show table columns in the object tree",
            Location = new Point(12, 25),
            AutoSize = true,
            Checked = settings.ShowTableColumnsInTree
        };
        groupObjectTree.Controls.Add(_chkShowColumns);

        var btnOk = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(192, 90),
            Size = new Size(75, 28)
        };
        btnOk.Click += (_, _) =>
        {
            Settings.ShowTableColumnsInTree = _chkShowColumns.Checked;
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(273, 90),
            Size = new Size(75, 28)
        };

        Controls.Add(groupObjectTree);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
