namespace SQL_prototipo.UI;

/// <summary>
/// A <see cref="DataGridView"/> with double buffering enabled to eliminate the
/// flicker that occurs while scrolling, resizing, or refreshing large result
/// sets. <see cref="DataGridView.DoubleBuffered"/> is protected, so it can only
/// be turned on from a derived type.
/// </summary>
public sealed class BufferedDataGridView : DataGridView
{
    public BufferedDataGridView()
    {
        DoubleBuffered = true;
    }
}
