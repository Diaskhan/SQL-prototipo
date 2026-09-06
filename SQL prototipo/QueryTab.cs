using SQL_prototipo.UI;

namespace SQL_prototipo;

public partial class MainForm
{
    private void OpenNewQueryTab(string query, string tableName, bool autoExecute = true)
    {
        // Build a unique tab title
        int tabCount = tabControl1.TabPages.Cast<TabPage>()
            .Count(tp => tp.Tag is QueryTabContext);
        string title = $"{tableName} ({tabCount + 1})";

        // --- Scintilla (query editor with SQL syntax highlighting) ---
        var rtb = new ScintillaNET.Scintilla
        {
            Dock = DockStyle.Fill,
            BorderStyle = ScintillaNET.BorderStyle.None
        };
        SetupSqlHighlighting(rtb);
        rtb.Text = query;

        // --- DataGridView (results) ---
        var dgv = new BufferedDataGridView
        {
            Dock = DockStyle.Bottom,
            BorderStyle = BorderStyle.None,
            Height = 250,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            ReadOnly = true,
            AllowUserToAddRows = false
        };

        // --- Splitter between query editor and results grid ---
        var splitter = new Splitter
        {
            Dock = DockStyle.Bottom,
            Height = 4,
            MinExtra = 100,
            MinSize = 80,
            TabStop = false
        };

        // --- Execute button ---
        var btnExec = new Button
        {
            Text = "Execute Query",
            Dock = DockStyle.Left,
            Width = 142,
            Height = 50
        };

        // --- Cancel button ---
        var btnCancel = new Button
        {
            Text = "Cancel",
            Dock = DockStyle.Left,
            Width = 100,
            Height = 50,
            Enabled = false
        };

        // --- Close tab button ---
        var btnClose = new Button
        {
            Text = "✕ Close Tab",
            Dock = DockStyle.Right,
            Width = 100,
            Height = 50
        };

        // --- Toolbar panel ---
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };
        toolbar.Controls.Add(btnCancel);
        toolbar.Controls.Add(btnExec);
        toolbar.Controls.Add(btnClose);

        // --- New TabPage ---
        var newTab = new TabPage(title)
        {
            Padding = new Padding(3),
            UseVisualStyleBackColor = true
        };

        var ctx = new QueryTabContext(rtb, dgv, btnExec, btnCancel);
        // Store references so F5 and ToggleUiState can find the right context
        newTab.Tag = ctx;

        newTab.Controls.Add(rtb);
        newTab.Controls.Add(splitter);
        newTab.Controls.Add(dgv);
        newTab.Controls.Add(toolbar);

        tabControl1.TabPages.Add(newTab);
        tabControl1.SelectedTab = newTab;

        // Make the query editor and the results grid share the height 50/50.
        void SizeGridToHalf()
        {
            int available = newTab.ClientSize.Height - toolbar.Height - splitter.Height;
            if (available > 0)
            {
                dgv.Height = available / 2;
            }
        }

        // Apply the initial 50/50 split once the tab has a real size, then stop
        // so the user's manual splitter drags are preserved on later resizes.
        void OnFirstSize(object? sender, EventArgs e)
        {
            if (newTab.ClientSize.Height <= 0) return;
            SizeGridToHalf();
            newTab.SizeChanged -= OnFirstSize;
        }
        newTab.SizeChanged += OnFirstSize;
        SizeGridToHalf();

        // Wire up events
        btnExec.Click += (_, _) => ExecuteQueryInTab(ctx);
        btnCancel.Click += (_, _) => ctx.Cts?.Cancel();
        btnClose.Click += (_, _) =>
        {
            ctx.Cts?.Cancel();
            tabControl1.TabPages.Remove(newTab);
            newTab.Dispose();
        };

        // Auto-execute on open
        if (autoExecute)
        {
            ExecuteQueryInTab(ctx);
        }
    }

    private async void ExecuteQueryInTab(QueryTabContext ctx)
    {
        var query = ctx.Editor.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        using var cts = new CancellationTokenSource();
        ctx.Cts = cts;
        try
        {
            ToggleUiState(false);
            SetStatus("Executing query...");

            var result = await _dbService.ExecuteQueryAsync(query, cts.Token);
            ApplyQueryResult(result, ctx.Grid, query);
        }
        catch (OperationCanceledException)
        {
            RecordHistory(query, false);
            SetStatus("Query canceled.");
        }
        catch (Exception ex)
        {
            RecordHistory(query, false);
            SetStatus("Query failed.");
            MessageBox.Show($"Error executing query: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ctx.Cts = null;
            ToggleUiState(true);
        }
    }

    private void SetupSqlHighlighting(ScintillaNET.Scintilla editor)
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

    private sealed class QueryTabContext(ScintillaNET.Scintilla editor, BufferedDataGridView grid, Button executeButton, Button cancelButton)
    {
        public ScintillaNET.Scintilla Editor { get; } = editor;
        public BufferedDataGridView Grid { get; } = grid;
        public Button ExecuteButton { get; } = executeButton;
        public Button CancelButton { get; } = cancelButton;
        public CancellationTokenSource? Cts { get; set; }
    }
}
