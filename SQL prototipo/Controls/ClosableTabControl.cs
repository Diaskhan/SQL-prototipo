using System.Drawing.Drawing2D;

namespace SQL_prototipo.Controls;

/// <summary>
/// A flat-styled <see cref="TabControl"/> (VS Code / Azure Data Studio look) that
/// draws each tab header itself with a close ("x") button and raises
/// <see cref="TabClosing"/> when it is clicked. All rendering is encapsulated here.
/// </summary>
public class ClosableTabControl : TabControl
{
    private const int CloseButtonSize = 16;
    private const int WM_PAINT = 0x000F;

    private static readonly Color AccentColor = Color.FromArgb(0, 122, 204);
    private static readonly Color SelectedBackColor = Color.White;
    private static readonly Color NormalBackColor = Color.FromArgb(236, 236, 236);
    private static readonly Color SeparatorColor = Color.FromArgb(200, 200, 200);
    private static readonly Color TextColor = Color.FromArgb(30, 30, 30);

    /// <summary>Raised when a tab's close button is clicked. Set
    /// <see cref="TabClosingEventArgs.Cancel"/> to true to prevent closing.</summary>
    public event EventHandler<TabClosingEventArgs>? TabClosing;

    public ClosableTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Normal;
        Appearance = TabAppearance.Normal;
        ItemSize = new Size(0, 26);
        // Reserve room on the right of each tab header for the close glyph.
        Padding = new Point(6 + CloseButtonSize, 3);
        DoubleBuffered = true;
    }

    private Rectangle GetCloseButtonRect(int index)
    {
        var tabRect = GetTabRect(index);
        int size = CloseButtonSize - 4;
        return new Rectangle(
            tabRect.Right - CloseButtonSize,
            tabRect.Top + (tabRect.Height - size) / 2,
            size,
            size);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
            return;

        for (int i = 0; i < TabPages.Count; i++)
        {
            if (GetCloseButtonRect(i).Contains(e.Location))
            {
                var page = TabPages[i];
                var args = new TabClosingEventArgs(page);
                TabClosing?.Invoke(this, args);

                if (!args.Cancel)
                {
                    TabPages.Remove(page);
                    page.Dispose();
                }
                break;
            }
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        // Repaint the whole header on top of the system-drawn 3D tabs to get a
        // fully flat appearance.
        if (m.Msg == WM_PAINT && TabCount > 0)
        {
            using var g = Graphics.FromHwnd(Handle);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            PaintHeader(g);
            PaintBodyBorder(g);
        }
    }

    private void PaintHeader(Graphics g)
    {
        var firstTab = GetTabRect(0);
        int stripTop = firstTab.Top;
        int stripBottom = firstTab.Bottom;

        // Flat base for the entire header row (covers system 3D frames and gaps).
        using (var stripBrush = new SolidBrush(NormalBackColor))
        {
            g.FillRectangle(stripBrush, 0, stripTop, Width, stripBottom - stripTop);
        }

        for (int i = 0; i < TabCount; i++)
        {
            DrawTab(g, i);
        }

        // Thin flat line under the whole tab strip.
        using var underline = new Pen(SeparatorColor, 1);
        g.DrawLine(underline, 0, stripBottom - 1, Width, stripBottom - 1);
    }

    private void DrawTab(Graphics g, int index)
    {
        var tabPage = TabPages[index];
        var tabRect = GetTabRect(index);
        bool isSelected = SelectedIndex == index;

        // Flat background: white for the active tab, light gray otherwise.
        using (var backBrush = new SolidBrush(isSelected ? SelectedBackColor : NormalBackColor))
        {
            g.FillRectangle(backBrush, tabRect);
        }

        // Vertical separators between tabs.
        using (var sep = new Pen(SeparatorColor, 1))
        {
            g.DrawLine(sep, tabRect.Right - 1, tabRect.Top + 4, tabRect.Right - 1, tabRect.Bottom - 4);
        }

        // Blue strip on top of the active tab instead of a full blue fill.
        if (isSelected)
        {
            using var accentBrush = new SolidBrush(AccentColor);
            g.FillRectangle(accentBrush, tabRect.Left, tabRect.Top, tabRect.Width, 2);
        }

        // Tab caption, leaving room for the close glyph.
        var textRect = new Rectangle(
            tabRect.Left + 6,
            tabRect.Top,
            tabRect.Width - CloseButtonSize - 8,
            tabRect.Height);
        TextRenderer.DrawText(
            g,
            tabPage.Text,
            Font,
            textRect,
            TextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        // Close ("x") glyph.
        var closeRect = GetCloseButtonRect(index);
        using var pen = new Pen(Color.FromArgb(120, 120, 120), 1.4f);
        int pad = 3;
        g.DrawLine(pen, closeRect.Left + pad, closeRect.Top + pad, closeRect.Right - pad, closeRect.Bottom - pad);
        g.DrawLine(pen, closeRect.Right - pad, closeRect.Top + pad, closeRect.Left + pad, closeRect.Bottom - pad);
    }

    private void PaintBodyBorder(Graphics g)
    {
        var display = DisplayRectangle;

        // Erase the sunken 3D border with the background color.
        using (var erasePen = new Pen(BackColor, 3))
        {
            g.DrawRectangle(erasePen, Rectangle.Inflate(display, 2, 2));
        }

        // Flat 1px border around the content area.
        using var borderPen = new Pen(SeparatorColor, 1);
        g.DrawRectangle(borderPen, new Rectangle(
            display.Left - 1,
            display.Top - 1,
            display.Width + 1,
            display.Height + 1));
    }
}

/// <summary>Event data for <see cref="ClosableTabControl.TabClosing"/>.</summary>
public class TabClosingEventArgs : EventArgs
{
    public TabClosingEventArgs(TabPage tabPage) => TabPage = tabPage;

    /// <summary>The tab page whose close button was clicked.</summary>
    public TabPage TabPage { get; }

    /// <summary>Set to true to cancel the default close/dispose behavior.</summary>
    public bool Cancel { get; set; }
}
