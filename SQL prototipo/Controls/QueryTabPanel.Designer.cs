namespace SQL_prototipo.Controls
{
    partial class QueryTabPanel
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (_grid?.DataSource as System.Data.DataTable)?.Dispose();
                _grid.DataSource = null;

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            _editor = new ScintillaNET.Scintilla();
            _grid = new BufferedDataGridView();
            _splitter = new Splitter();
            _btnExec = new Button();
            _btnCancel = new Button();
            _btnClose = new Button();
            _toolbar = new Panel();
            ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
            _toolbar.SuspendLayout();
            SuspendLayout();
            // 
            // _editor
            // 
            _editor.AutoCMaxHeight = 9;
            _editor.BiDirectionality = ScintillaNET.BiDirectionalDisplayType.Disabled;
            _editor.BorderStyle = ScintillaNET.BorderStyle.None;
            _editor.CaretLineBackColor = Color.Honeydew;
            _editor.CaretLineVisible = true;
            _editor.Dock = DockStyle.Fill;
            _editor.LexerName = null;
            _editor.Location = new Point(0, 50);
            _editor.Name = "_editor";
            _editor.ScrollWidth = 1;
            _editor.Size = new Size(881, 292);
            _editor.TabIndents = true;
            _editor.TabIndex = 0;
            _editor.UseRightToLeftReadingLayout = false;
            _editor.WrapMode = ScintillaNET.WrapMode.None;
            // 
            // _grid
            // 
            _grid.AllowUserToAddRows = false;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(245, 245, 245);
            _grid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            _grid.BorderStyle = BorderStyle.None;
            _grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _grid.Dock = DockStyle.Bottom;
            _grid.Location = new Point(0, 346);
            _grid.Name = "_grid";
            _grid.ReadOnly = true;
            dataGridViewCellStyle2.BackColor = Color.White;
            _grid.RowsDefaultCellStyle = dataGridViewCellStyle2;
            _grid.Size = new Size(881, 201);
            _grid.TabIndex = 2;
            _grid.CellContentClick += _grid_CellContentClick;
            // 
            // _splitter
            // 
            _splitter.Dock = DockStyle.Bottom;
            _splitter.Location = new Point(0, 342);
            _splitter.MinExtra = 100;
            _splitter.MinSize = 80;
            _splitter.Name = "_splitter";
            _splitter.Size = new Size(881, 4);
            _splitter.TabIndex = 1;
            _splitter.TabStop = false;
            // 
            // _btnExec
            // 
            _btnExec.Dock = DockStyle.Left;
            _btnExec.Location = new Point(0, 0);
            _btnExec.Name = "_btnExec";
            _btnExec.Size = new Size(142, 50);
            _btnExec.TabIndex = 1;
            _btnExec.Text = "Execute Query";
            // 
            // _btnCancel
            // 
            _btnCancel.Dock = DockStyle.Left;
            _btnCancel.Enabled = false;
            _btnCancel.Location = new Point(142, 0);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(100, 50);
            _btnCancel.TabIndex = 0;
            _btnCancel.Text = "Cancel";
            // 
            // _btnClose
            // 
            _btnClose.Dock = DockStyle.Right;
            _btnClose.Location = new Point(781, 0);
            _btnClose.Name = "_btnClose";
            _btnClose.Size = new Size(100, 50);
            _btnClose.TabIndex = 2;
            _btnClose.Text = "✕ Close Tab";
            // 
            // _toolbar
            // 
            _toolbar.Controls.Add(_btnCancel);
            _toolbar.Controls.Add(_btnExec);
            _toolbar.Controls.Add(_btnClose);
            _toolbar.Dock = DockStyle.Top;
            _toolbar.Location = new Point(0, 0);
            _toolbar.Name = "_toolbar";
            _toolbar.Size = new Size(881, 50);
            _toolbar.TabIndex = 3;
            // 
            // QueryTabPanel
            // 
            Controls.Add(_editor);
            Controls.Add(_splitter);
            Controls.Add(_grid);
            Controls.Add(_toolbar);
            Name = "QueryTabPanel";
            Size = new Size(881, 547);
            ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
            _toolbar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private ScintillaNET.Scintilla _editor;
        private BufferedDataGridView _grid;
        private Splitter _splitter;
        private Button _btnExec;
        private Button _btnCancel;
        private Button _btnClose;
        private Panel _toolbar;
    }
}
