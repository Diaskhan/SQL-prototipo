using SQL_prototipo.UI;

namespace SQL_prototipo
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            leftTabControl = new TabControl();
            tabPageDatabase = new TabPage();
            treeView1 = new TreeView();
            txtTableFilter = new TextBox();
            tabPageQueries = new TabPage();
            splitterMain = new Splitter();
            splitterQuery = new Splitter();
            panel1 = new Panel();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            richTextBox1 = new ScintillaNET.Scintilla();
            dataGridView1 = new BufferedDataGridView();
            panel3 = new Panel();
            button2 = new Button();
            button1 = new Button();
            tabPage2 = new TabPage();
            connectionsPanel = new Controls.ConnectionsPanel();
            menuStrip1 = new MenuStrip();
            fileMenuItem = new ToolStripMenuItem();
            newQueryMenuItem = new ToolStripMenuItem();
            executeQueryMenuItem = new ToolStripMenuItem();
            settingsMenuItem = new ToolStripMenuItem();
            exitMenuItem = new ToolStripMenuItem();
            helpMenuItem = new ToolStripMenuItem();
            aboutMenuItem = new ToolStripMenuItem();
            leftTabControl.SuspendLayout();
            tabPageDatabase.SuspendLayout();
            panel1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel3.SuspendLayout();
            tabPage2.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // leftTabControl
            // 
            leftTabControl.Controls.Add(tabPageDatabase);
            leftTabControl.Controls.Add(tabPageQueries);
            leftTabControl.Dock = DockStyle.Left;
            leftTabControl.ItemSize = new Size(160, 30);
            leftTabControl.Location = new Point(0, 24);
            leftTabControl.Name = "leftTabControl";
            leftTabControl.SelectedIndex = 0;
            leftTabControl.Size = new Size(324, 724);
            leftTabControl.SizeMode = TabSizeMode.Fixed;
            leftTabControl.TabIndex = 0;
            // 
            // tabPageDatabase
            // 
            tabPageDatabase.Controls.Add(treeView1);
            tabPageDatabase.Controls.Add(txtTableFilter);
            tabPageDatabase.Location = new Point(4, 34);
            tabPageDatabase.Name = "tabPageDatabase";
            tabPageDatabase.Padding = new Padding(3);
            tabPageDatabase.Size = new Size(316, 686);
            tabPageDatabase.TabIndex = 0;
            tabPageDatabase.Text = "Database";
            tabPageDatabase.UseVisualStyleBackColor = true;
            // 
            // treeView1
            // 
            treeView1.BorderStyle = BorderStyle.None;
            treeView1.Dock = DockStyle.Fill;
            treeView1.Location = new Point(3, 26);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(310, 657);
            treeView1.TabIndex = 0;
            // 
            // txtTableFilter
            // 
            txtTableFilter.Dock = DockStyle.Top;
            txtTableFilter.Location = new Point(3, 3);
            txtTableFilter.Name = "txtTableFilter";
            txtTableFilter.PlaceholderText = "Filter tables... (e.g. *invoice*)";
            txtTableFilter.Size = new Size(310, 23);
            txtTableFilter.TabIndex = 1;
            // 
            // tabPageQueries
            // 
            tabPageQueries.Location = new Point(4, 34);
            tabPageQueries.Name = "tabPageQueries";
            tabPageQueries.Padding = new Padding(3);
            tabPageQueries.Size = new Size(316, 686);
            tabPageQueries.TabIndex = 1;
            tabPageQueries.Text = "Queries";
            tabPageQueries.UseVisualStyleBackColor = true;
            // 
            // splitterMain
            // 
            splitterMain.Location = new Point(324, 24);
            splitterMain.MinExtra = 200;
            splitterMain.MinSize = 150;
            splitterMain.Name = "splitterMain";
            splitterMain.Size = new Size(4, 724);
            splitterMain.TabIndex = 4;
            splitterMain.TabStop = false;
            // 
            // splitterQuery
            // 
            splitterQuery.Dock = DockStyle.Bottom;
            splitterQuery.Location = new Point(3, 254);
            splitterQuery.MinExtra = 100;
            splitterQuery.MinSize = 80;
            splitterQuery.Name = "splitterQuery";
            splitterQuery.Size = new Size(966, 14);
            splitterQuery.TabIndex = 3;
            splitterQuery.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.ButtonFace;
            panel1.Controls.Add(tabControl1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(328, 24);
            panel1.Name = "panel1";
            panel1.Size = new Size(980, 724);
            panel1.TabIndex = 1;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(980, 724);
            tabControl1.TabIndex = 3;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(richTextBox1);
            tabPage1.Controls.Add(splitterQuery);
            tabPage1.Controls.Add(dataGridView1);
            tabPage1.Controls.Add(panel3);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(972, 696);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Query";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Location = new Point(3, 53);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(966, 201);
            richTextBox1.TabIndex = 0;
            // 
            // dataGridView1
            // 
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Dock = DockStyle.Bottom;
            dataGridView1.Location = new Point(3, 268);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(966, 425);
            dataGridView1.TabIndex = 2;
            // 
            // panel3
            // 
            panel3.Controls.Add(button2);
            panel3.Controls.Add(button1);
            panel3.Dock = DockStyle.Top;
            panel3.Location = new Point(3, 3);
            panel3.Name = "panel3";
            panel3.Size = new Size(966, 50);
            panel3.TabIndex = 1;
            // 
            // button2
            // 
            button2.Dock = DockStyle.Left;
            button2.Location = new Point(142, 0);
            button2.Name = "button2";
            button2.Size = new Size(142, 50);
            button2.TabIndex = 3;
            button2.Text = "Execute Query";
            button2.UseVisualStyleBackColor = true;
            button2.Click += BtnExecuteQuery_Click;
            // 
            // button1
            // 
            button1.Dock = DockStyle.Left;
            button1.Location = new Point(0, 0);
            button1.Name = "button1";
            button1.Size = new Size(142, 50);
            button1.TabIndex = 2;
            button1.Text = "Load Tables";
            button1.UseVisualStyleBackColor = true;
            button1.Click += BtnLoadTables_Click;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(connectionsPanel);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(972, 696);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Connections";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // connectionsPanel
            // 
            connectionsPanel.Dock = DockStyle.Fill;
            connectionsPanel.Location = new Point(3, 3);
            connectionsPanel.Name = "connectionsPanel";
            connectionsPanel.Size = new Size(966, 690);
            connectionsPanel.TabIndex = 0;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileMenuItem, helpMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1308, 24);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileMenuItem
            // 
            fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newQueryMenuItem, executeQueryMenuItem, settingsMenuItem, exitMenuItem });
            fileMenuItem.Name = "fileMenuItem";
            fileMenuItem.Size = new Size(37, 20);
            fileMenuItem.Text = "&File";
            // 
            // newQueryMenuItem
            // 
            newQueryMenuItem.Name = "newQueryMenuItem";
            newQueryMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newQueryMenuItem.Size = new Size(176, 22);
            newQueryMenuItem.Text = "&New Query";
            newQueryMenuItem.Click += NewQueryMenuItem_Click;
            // 
            // executeQueryMenuItem
            // 
            executeQueryMenuItem.Name = "executeQueryMenuItem";
            executeQueryMenuItem.ShortcutKeys = Keys.F5;
            executeQueryMenuItem.Size = new Size(176, 22);
            executeQueryMenuItem.Text = "&Execute Query";
            executeQueryMenuItem.Click += ExecuteQueryMenuItem_Click;
            // 
            // settingsMenuItem
            // 
            settingsMenuItem.Name = "settingsMenuItem";
            settingsMenuItem.Size = new Size(176, 22);
            settingsMenuItem.Text = "&Settings...";
            settingsMenuItem.Click += SettingsMenuItem_Click;
            // 
            // exitMenuItem
            // 
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.Size = new Size(176, 22);
            exitMenuItem.Text = "E&xit";
            exitMenuItem.Click += ExitMenuItem_Click;
            // 
            // helpMenuItem
            // 
            helpMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutMenuItem });
            helpMenuItem.Name = "helpMenuItem";
            helpMenuItem.Size = new Size(44, 20);
            helpMenuItem.Text = "&Help";
            // 
            // aboutMenuItem
            // 
            aboutMenuItem.Name = "aboutMenuItem";
            aboutMenuItem.Size = new Size(107, 22);
            aboutMenuItem.Text = "&About";
            aboutMenuItem.Click += AboutMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1308, 748);
            Controls.Add(panel1);
            Controls.Add(splitterMain);
            Controls.Add(leftTabControl);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Text = "Database Manager";
            leftTabControl.ResumeLayout(false);
            tabPageDatabase.ResumeLayout(false);
            tabPageDatabase.PerformLayout();
            panel1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel3.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }


        #endregion

        private TreeView treeView1;
        private TextBox txtTableFilter;
        private Panel panel1;
        private Splitter splitterMain;
        private Splitter splitterQuery;
        private TabControl leftTabControl;
        private TabPage tabPageDatabase;
        private TabPage tabPageQueries;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private ScintillaNET.Scintilla richTextBox1;
        private Panel panel3;
        private Button button1;
        private Button button2;
        private Controls.ConnectionsPanel connectionsPanel;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem newQueryMenuItem;
        private ToolStripMenuItem executeQueryMenuItem;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem settingsMenuItem;
        private ToolStripMenuItem helpMenuItem;
        private ToolStripMenuItem aboutMenuItem;
        private BufferedDataGridView dataGridView1;
    }
}
