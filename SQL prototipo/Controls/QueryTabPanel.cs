using SQL_prototipo.Models;
using SQL_prototipo.Services;

namespace SQL_prototipo.Controls;

/// <summary>
/// Self-contained UI for a single SQL query tab: a syntax-highlighted editor,
/// a results grid, and execute/cancel/close controls. Owns its own execution
/// logic against a shared <see cref="DatabaseService"/> supplied by the host,
/// and communicates back to the host form through events.
/// </summary>
public partial class QueryTabPanel : UserControl
{
    private Func<DatabaseService>? _dbServiceProvider;
    private CancellationTokenSource? _cts;
    private bool _userAdjustedSplit;

    /// <summary>Raised to report a short status message to the host.</summary>
    public event EventHandler<string>? StatusChanged;

    /// <summary>Raised when a query starts (true) or finishes (false) executing.</summary>
    public event EventHandler<bool>? BusyChanged;

    /// <summary>Raised after a query finishes so the host can record history.</summary>
    public event EventHandler<QueryCompletedEventArgs>? QueryCompleted;

    /// <summary>Raised when the user clicks the "Close Tab" button.</summary>
    public event EventHandler? CloseRequested;

    public QueryTabPanel()
    {
        InitializeComponent();

        // Scintilla relies on native components that are not available inside the
        // Windows Forms designer, so only apply syntax highlighting at runtime.
        if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Runtime)
        {
            ApplySqlHighlighting(_editor);
        }

        // Wire up events
        _btnExec.Click += (_, _) => ExecuteQuery();
        _btnCancel.Click += (_, _) => CancelQuery();
        _btnClose.Click += (_, _) =>
        {
            CancelQuery();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        };

        SizeChanged += QueryTabPanel_SizeChanged;
        _splitter.SplitterMoved += (_, _) => _userAdjustedSplit = true;
    }

    /// <summary>
    /// Creates a query panel, hosts it in a new page of <paramref name="tabControl"/>,
    /// wires it to the host callbacks, and optionally runs the query immediately.
    /// </summary>
    public static QueryTabPanel Open(
        TabControl tabControl,
        string query,
        string tableName,
        Func<DatabaseService> dbServiceProvider,
        Action<string> onStatus,
        Action<bool> onBusy,
        Action<string, bool> onQueryCompleted,
        bool autoExecute = true)
    {
        // Build a unique tab title
        int tabCount = tabControl.TabPages.Cast<TabPage>()
            .Count(tp => tp.Tag is QueryTabPanel);
        string title = $"{tableName} ({tabCount + 1})";

        var panel = new QueryTabPanel { Dock = DockStyle.Fill };
        panel.Initialize(dbServiceProvider, query);
        panel.StatusChanged += (_, message) => onStatus(message);
        panel.BusyChanged += (_, busy) => onBusy(busy);
        panel.QueryCompleted += (_, e) => onQueryCompleted(e.Query, e.Success);

        // Store the panel so F5 and ToggleUiState can find the right tab
        var newTab = new TabPage(title)
        {
            Padding = new Padding(3),
            UseVisualStyleBackColor = true,
            Tag = panel
        };

        panel.CloseRequested += (_, _) =>
        {
            tabControl.TabPages.Remove(newTab);
            newTab.Dispose();
        };

        newTab.Controls.Add(panel);
        tabControl.TabPages.Add(newTab);
        tabControl.SelectedTab = newTab;

        if (autoExecute)
        {
            panel.ExecuteQuery();
        }

        return panel;
    }

    /// <summary>Wires the panel to a live database-service provider and loads the initial query text.</summary>
    public void Initialize(Func<DatabaseService> dbServiceProvider, string query)
    {
        _dbServiceProvider = dbServiceProvider;
        _editor.Text = query;
    }

    /// <summary>The current (trimmed) query text.</summary>
    public string QueryText => _editor.Text.Trim();

    /// <summary>Enables/disables the Execute button and mirrors the inverse on Cancel.</summary>
    public void SetExecutionEnabled(bool enabled)
    {
        _btnExec.Enabled = enabled;
        _btnCancel.Enabled = !enabled;
    }

    /// <summary>Requests cancellation of the currently running query, if any.</summary>
    public void CancelQuery() => _cts?.Cancel();

    /// <summary>Executes the current query text against the provided database service.</summary>
    public async void ExecuteQuery()
    {
        var query = QueryText;
        if (string.IsNullOrEmpty(query) || _dbServiceProvider == null) return;

        using var cts = new CancellationTokenSource();
        _cts = cts;
        try
        {
            SetBusy(true);
            OnStatus("Executing query...");

            var result = await _dbServiceProvider().ExecuteQueryAsync(query, cts.Token);
            ApplyQueryResult(result);
            OnQueryCompleted(query, true);
        }
        catch (OperationCanceledException)
        {
            OnQueryCompleted(query, false);
            OnStatus("Query canceled.");
        }
        catch (Exception ex)
        {
            OnQueryCompleted(query, false);
            OnStatus("Query failed.");
            MessageBox.Show($"Error executing query: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cts = null;
            SetBusy(false);
        }
    }

    private void ApplyQueryResult(QueryResult result)
    {
        if (result.HasResultSet)
        {
            _grid.PrepareForDataRefresh();
            SetGridDataSource(result.Data);
            _grid.ConfigureBinaryColumns(result.Data);
            OnStatus($"{result.RowCount} row(s) returned in {result.ElapsedMilliseconds} ms.");
        }
        else
        {
            SetGridDataSource(null);
            OnStatus($"{result.RecordsAffected} row(s) affected in {result.ElapsedMilliseconds} ms.");
        }
    }

    // Replaces the grid's data source, disposing the previously bound DataTable so its
    // memory is released immediately instead of lingering until the next GC pass.
    private void SetGridDataSource(System.Data.DataTable? data)
    {
        var previous = _grid.DataSource as System.Data.DataTable;
        _grid.DataSource = data;
        if (previous != null && !ReferenceEquals(previous, data))
        {
            previous.Dispose();
        }
    }

    private void SetBusy(bool busy)
    {
        SetExecutionEnabled(!busy);
        BusyChanged?.Invoke(this, busy);
    }

    private void OnStatus(string message) => StatusChanged?.Invoke(this, message);

    private void OnQueryCompleted(string query, bool success)
        => QueryCompleted?.Invoke(this, new QueryCompletedEventArgs(query, success));

    private void QueryTabPanel_SizeChanged(object? sender, EventArgs e)
    {
        if (ClientSize.Height <= 0) return;

        // Keep the editor and results grid at a 50/50 split through the layout
        // passes and window resizes, until the user manually drags the splitter
        // (after which their chosen size is preserved).
        if (!_userAdjustedSplit)
        {
            SizeGridToHalf();
        }
    }

    // Make the query editor and the results grid share the height 50/50.
    private void SizeGridToHalf()
    {
        int available = ClientSize.Height - _toolbar.Height - _splitter.Height;
        if (available > 0)
        {
            _grid.Height = available / 2;
        }
    }


    /// <summary>Applies SQL syntax highlighting to a Scintilla editor.</summary>
    public static void ApplySqlHighlighting(ScintillaNET.Scintilla editor)
    {
        // Enable the built-in SQL lexer
        editor.LexerName = "sql";

        // Base font/style for all styles
        editor.StyleResetDefault();
        editor.Styles[ScintillaNET.Style.Default].Font = "Consolas";
        editor.Styles[ScintillaNET.Style.Default].Size = 11;
        editor.StyleClearAll();

        // Case-insensitive keyword matching
        editor.SetProperty("sql.case.sensitive.keywords", "0");

        // Colors for individual SQL styles
        editor.Styles[ScintillaNET.Style.Sql.Comment].ForeColor = System.Drawing.Color.Green;
        editor.Styles[ScintillaNET.Style.Sql.CommentLine].ForeColor = System.Drawing.Color.Green;
        editor.Styles[ScintillaNET.Style.Sql.CommentDoc].ForeColor = System.Drawing.Color.Green;
        editor.Styles[ScintillaNET.Style.Sql.Number].ForeColor = System.Drawing.Color.Olive;
        editor.Styles[ScintillaNET.Style.Sql.Word].ForeColor = System.Drawing.Color.Blue;      // keywords
        editor.Styles[ScintillaNET.Style.Sql.Word2].ForeColor = System.Drawing.Color.DarkCyan; // functions/types
        editor.Styles[ScintillaNET.Style.Sql.String].ForeColor = System.Drawing.Color.Firebrick;
        editor.Styles[ScintillaNET.Style.Sql.Character].ForeColor = System.Drawing.Color.Firebrick;
        editor.Styles[ScintillaNET.Style.Sql.Operator].ForeColor = System.Drawing.Color.Black;
        editor.Styles[ScintillaNET.Style.Sql.Identifier].ForeColor = System.Drawing.Color.Black;

        // Keyword set 0 -> Style.Sql.Word
        editor.SetKeywords(0,
            "select insert update delete from where join inner left right outer full cross " +
            "on group by having order asc desc distinct as and or not null is in like between " +
            "exists union all create table alter drop truncate index view into values set " +
            "primary key foreign references default constraint unique check case when then else end " +
            "limit offset top with");

        // Keyword set 1 -> Style.Sql.Word2 (functions/types)
        editor.SetKeywords(1,
            "count sum avg min max coalesce nullif cast convert getdate now datediff dateadd " +
            "substring len length upper lower trim ltrim rtrim replace round abs " +
            "int integer bigint smallint tinyint bit decimal numeric float real money " +
            "char varchar nvarchar nchar text datetime date time timestamp uniqueidentifier bool boolean");
    }

    private void _grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {

    }
}

/// <summary>Carries the executed query text and its success flag to the host.</summary>
public sealed class QueryCompletedEventArgs(string query, bool success) : EventArgs
{
    public string Query { get; } = query;
    public bool Success { get; } = success;
}
