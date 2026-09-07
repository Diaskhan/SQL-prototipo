using SQL_prototipo.Models;
using SQL_prototipo.Services;

namespace SQL_prototipo.Controls;

/// <summary>
/// Self-contained UI for the query history: an owner-drawn list grouped by day.
/// Owns its rendering against a shared <see cref="QueryHistoryManager"/> supplied
/// by the host, and raises <see cref="EntryActivated"/> when the user double-clicks
/// an entry so the host can load it into a new query tab.
/// </summary>
public partial class HistoryPanel : UserControl
{
    private QueryHistoryManager _historyManager = null!;

    /// <summary>Raised when the user double-clicks a history entry.</summary>
    public event EventHandler<QueryHistoryEntry>? EntryActivated;

    public HistoryPanel()
    {
        InitializeComponent();
    }

    /// <summary>Wires the panel to the shared history manager and performs the first load.</summary>
    public void Initialize(QueryHistoryManager historyManager)
    {
        _historyManager = historyManager;
        Reload();
    }

    /// <summary>Records a new entry through the manager and refreshes the list.</summary>
    public void Add(string query, string connectionName, bool success)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        _historyManager.Add(query, connectionName, success);
        Reload();
    }

    /// <summary>Rebuilds the history list (grouped by day) from the manager.</summary>
    public void Reload()
    {
        if (_historyManager == null) return;

        _listBoxHistory.BeginUpdate();
        _listBoxHistory.Items.Clear();

        DateTime? currentDay = null;
        foreach (var entry in _historyManager.GetRecent())
        {
            var day = entry.ExecutedAt.Date;
            if (currentDay == null || currentDay.Value != day)
            {
                currentDay = day;
                _listBoxHistory.Items.Add(new HistoryDayHeader(day));
            }
            _listBoxHistory.Items.Add(entry);
        }

        _listBoxHistory.EndUpdate();
    }

    private void ListBoxHistory_DoubleClick(object? sender, EventArgs e)
    {
        if (_listBoxHistory.SelectedItem is QueryHistoryEntry entry)
        {
            EntryActivated?.Invoke(this, entry);
        }
    }

    /// <summary>Marker item used to render a day separator inside the history list.</summary>
    private sealed class HistoryDayHeader(DateTime day)
    {
        public DateTime Day { get; } = day;

        public string Text
        {
            get
            {
                var today = DateTime.Now.Date;
                if (Day == today) return "Today";
                if (Day == today.AddDays(-1)) return "Yesterday";
                return Day.ToString("dddd, dd MMMM yyyy");
            }
        }
    }

    private void ListBoxHistory_MeasureItem(object? sender, MeasureItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _listBoxHistory.Items.Count) return;

        var item = _listBoxHistory.Items[e.Index];
        if (item is HistoryDayHeader)
        {
            e.ItemHeight = _listBoxHistory.Font.Height + 8;
        }
        else
        {
            // Two lines: meta line + query line.
            e.ItemHeight = _listBoxHistory.Font.Height * 2 + 8;
        }
    }

    private void ListBoxHistory_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _listBoxHistory.Items.Count) return;

        var item = _listBoxHistory.Items[e.Index];

        if (item is HistoryDayHeader header)
        {
            using var headerBg = new SolidBrush(Color.FromArgb(230, 230, 235));
            e.Graphics.FillRectangle(headerBg, e.Bounds);
            using var headerFont = new Font(e.Font!, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, header.Text, headerFont, e.Bounds,
                Color.FromArgb(60, 60, 60),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            using var pen = new Pen(Color.FromArgb(200, 200, 205));
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            return;
        }

        e.DrawBackground();

        if (item is QueryHistoryEntry entry)
        {
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var metaColor = selected ? SystemColors.HighlightText : Color.FromArgb(110, 110, 110);
            var queryColor = selected ? SystemColors.HighlightText : e.ForeColor;

            var status = entry.Success ? "OK" : "ERR";
            var meta = $"[{entry.ExecutedAt:HH:mm:ss}] ({status}) {entry.ConnectionName}";
            var query = entry.Query.Replace("\r", " ").Replace("\n", " ").Trim();

            var lineHeight = e.Font!.Height;
            var metaBounds = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 2, e.Bounds.Width - 8, lineHeight);
            var queryBounds = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top + 2 + lineHeight, e.Bounds.Width - 12, lineHeight);

            using var metaFont = new Font(e.Font, FontStyle.Regular);
            TextRenderer.DrawText(e.Graphics, meta, metaFont, metaBounds, metaColor,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, query, e.Font, queryBounds, queryColor,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        e.DrawFocusRectangle();
    }
}
