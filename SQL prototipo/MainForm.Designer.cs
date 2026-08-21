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
            treeView1 = new TreeView();
            panel1 = new Panel();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            dataGridView1 = new DataGridView();
            richTextBox1 = new RichTextBox();
            panel3 = new Panel();
            button2 = new Button();
            button1 = new Button();
            tabPage2 = new TabPage();
            panel4 = new Panel();
            labelGroup = new Label();
            txtGroup = new TextBox();
            label4 = new Label();
            cmbConnectionType = new ComboBox();
            label3 = new Label();
            cmbConnections = new ComboBox();
            txtConnectionString = new TextBox();
            label2 = new Label();
            txtConnectionName = new TextBox();
            label1 = new Label();
            btnDeleteConnection = new Button();
            btnAddConnection = new Button();
            btnAddFolder = new Button();
            treeViewConnections = new TreeView();
            menuStrip1 = new MenuStrip();
            fileMenuItem = new ToolStripMenuItem();
            newQueryMenuItem = new ToolStripMenuItem();
            executeQueryMenuItem = new ToolStripMenuItem();
            exitMenuItem = new ToolStripMenuItem();
            helpMenuItem = new ToolStripMenuItem();
            aboutMenuItem = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel3.SuspendLayout();
            tabPage2.SuspendLayout();
            panel4.SuspendLayout();
            SuspendLayout();
            // 
            // treeView1
            // 
            treeView1.Dock = DockStyle.Left;
            treeView1.Location = new Point(0, 0);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(324, 577);
            treeView1.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.ButtonFace;
            panel1.Controls.Add(tabControl1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(324, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(772, 577);
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
            tabControl1.Size = new Size(772, 577);
            tabControl1.TabIndex = 3;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(dataGridView1);
            tabPage1.Controls.Add(richTextBox1);
            tabPage1.Controls.Add(panel3);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(764, 549);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Query";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Dock = DockStyle.Bottom;
            dataGridView1.Location = new Point(3, 296);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(758, 250);
            dataGridView1.TabIndex = 2;
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Location = new Point(3, 53);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(758, 493);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = "";
            // 
            // panel3
            // 
            panel3.Controls.Add(button2);
            panel3.Controls.Add(button1);
            panel3.Dock = DockStyle.Top;
            panel3.Location = new Point(3, 3);
            panel3.Name = "panel3";
            panel3.Size = new Size(758, 50);
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
            button2.Click += btnExecuteQuery_Click;
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
            button1.Click += btnLoadTables_Click;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(panel4);
            tabPage2.Controls.Add(treeViewConnections);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(764, 549);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Connections";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // panel4
            // 
            panel4.Controls.Add(labelGroup);
            panel4.Controls.Add(txtGroup);
            panel4.Controls.Add(label4);
            panel4.Controls.Add(cmbConnectionType);
            panel4.Controls.Add(label3);
            panel4.Controls.Add(cmbConnections);
            panel4.Controls.Add(txtConnectionString);
            panel4.Controls.Add(label2);
            panel4.Controls.Add(txtConnectionName);
            panel4.Controls.Add(label1);
            panel4.Controls.Add(btnDeleteConnection);
            panel4.Controls.Add(btnAddConnection);
            panel4.Controls.Add(btnAddFolder);
            panel4.Dock = DockStyle.Right;
            panel4.Location = new Point(400, 3);
            panel4.Name = "panel4";
            panel4.Size = new Size(361, 543);
            panel4.TabIndex = 1;
            // 
            // labelGroup
            // 
            labelGroup.AutoSize = true;
            labelGroup.Location = new Point(10, 95);
            labelGroup.Name = "labelGroup";
            labelGroup.Size = new Size(40, 15);
            labelGroup.TabIndex = 11;
            labelGroup.Text = "Group";
            // 
            // txtGroup
            // 
            txtGroup.Location = new Point(10, 115);
            txtGroup.Name = "txtGroup";
            txtGroup.PlaceholderText = "e.g., Development, Production";
            txtGroup.Size = new Size(341, 23);
            txtGroup.TabIndex = 10;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(10, 145);
            label4.Name = "label4";
            label4.Size = new Size(83, 15);
            label4.TabIndex = 9;
            label4.Text = "Database Type";
            // 
            // cmbConnectionType
            // 
            cmbConnectionType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbConnectionType.FormattingEnabled = true;
            cmbConnectionType.Items.AddRange(new object[] { "SQLite", "SqlServer", "MySQL", "PostgreSQL" });
            cmbConnectionType.Location = new Point(10, 165);
            cmbConnectionType.Name = "cmbConnectionType";
            cmbConnectionType.Size = new Size(341, 23);
            cmbConnectionType.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(10, 195);
            label3.Name = "label3";
            label3.Size = new Size(107, 15);
            label3.TabIndex = 7;
            label3.Text = "Switch Connection";
            // 
            // cmbConnections
            // 
            cmbConnections.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbConnections.FormattingEnabled = true;
            cmbConnections.Location = new Point(10, 215);
            cmbConnections.Name = "cmbConnections";
            cmbConnections.Size = new Size(341, 23);
            cmbConnections.TabIndex = 6;
            cmbConnections.SelectedIndexChanged += cmbConnections_SelectedIndexChanged;
            // 
            // txtConnectionString
            // 
            txtConnectionString.Location = new Point(10, 70);
            txtConnectionString.Name = "txtConnectionString";
            txtConnectionString.PlaceholderText = "e.g., Data Source=mydb.sqlite";
            txtConnectionString.Size = new Size(341, 23);
            txtConnectionString.TabIndex = 5;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(10, 50);
            label2.Name = "label2";
            label2.Size = new Size(103, 15);
            label2.TabIndex = 4;
            label2.Text = "Connection String";
            // 
            // txtConnectionName
            // 
            txtConnectionName.Location = new Point(10, 25);
            txtConnectionName.Name = "txtConnectionName";
            txtConnectionName.PlaceholderText = "Connection name";
            txtConnectionName.Size = new Size(341, 23);
            txtConnectionName.TabIndex = 3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 5);
            label1.Name = "label1";
            label1.Size = new Size(74, 15);
            label1.TabIndex = 2;
            label1.Text = "Conn. Name";
            // 
            // btnDeleteConnection
            // 
            btnDeleteConnection.Location = new Point(186, 245);
            btnDeleteConnection.Name = "btnDeleteConnection";
            btnDeleteConnection.Size = new Size(165, 40);
            btnDeleteConnection.TabIndex = 1;
            btnDeleteConnection.Text = "Delete Connection";
            btnDeleteConnection.UseVisualStyleBackColor = true;
            btnDeleteConnection.Click += btnDeleteConnection_Click;
            // 
            // btnAddConnection
            // 
            btnAddConnection.Location = new Point(10, 245);
            btnAddConnection.Name = "btnAddConnection";
            btnAddConnection.Size = new Size(165, 40);
            btnAddConnection.TabIndex = 0;
			btnAddConnection.Text = "Add Connection";
			btnAddConnection.UseVisualStyleBackColor = true;
			btnAddConnection.Click += btnAddConnection_Click;
			// 
			// btnAddFolder
			// 
			btnAddFolder.Location = new Point(10, 295);
			btnAddFolder.Name = "btnAddFolder";
			btnAddFolder.Size = new Size(341, 40);
			btnAddFolder.TabIndex = 12;
			btnAddFolder.Text = "Add Folder";
			btnAddFolder.UseVisualStyleBackColor = true;
			btnAddFolder.Click += btnAddFolder_Click;
			// 
			// treeViewConnections
			// 
			treeViewConnections.Dock = DockStyle.Fill;
			treeViewConnections.HideSelection = false;
			treeViewConnections.Location = new Point(3, 3);
			treeViewConnections.Name = "treeViewConnections";
			treeViewConnections.Size = new Size(758, 543);
			treeViewConnections.TabIndex = 0;
			treeViewConnections.AfterSelect += treeViewConnections_AfterSelect;
			treeViewConnections.NodeMouseDoubleClick += treeViewConnections_NodeMouseDoubleClick;
			// 
			// menuStrip1
			// 
			menuStrip1.Items.AddRange(new ToolStripItem[] { fileMenuItem, helpMenuItem });
			menuStrip1.Location = new Point(0, 0);
			menuStrip1.Name = "menuStrip1";
			menuStrip1.Size = new Size(1096, 24);
			menuStrip1.TabIndex = 2;
			menuStrip1.Text = "menuStrip1";
			// 
			// fileMenuItem
			// 
			fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newQueryMenuItem, executeQueryMenuItem, new ToolStripSeparator(), exitMenuItem });
			fileMenuItem.Name = "fileMenuItem";
			fileMenuItem.Size = new Size(37, 20);
			fileMenuItem.Text = "&File";
			// 
			// newQueryMenuItem
			// 
			newQueryMenuItem.Name = "newQueryMenuItem";
			newQueryMenuItem.ShortcutKeys = Keys.Control | Keys.N;
			newQueryMenuItem.Size = new Size(180, 22);
			newQueryMenuItem.Text = "&New Query";
			newQueryMenuItem.Click += newQueryMenuItem_Click;
			// 
			// executeQueryMenuItem
			// 
			executeQueryMenuItem.Name = "executeQueryMenuItem";
			executeQueryMenuItem.ShortcutKeys = Keys.F5;
			executeQueryMenuItem.Size = new Size(180, 22);
			executeQueryMenuItem.Text = "&Execute Query";
			executeQueryMenuItem.Click += executeQueryMenuItem_Click;
			// 
			// exitMenuItem
			// 
			exitMenuItem.Name = "exitMenuItem";
			exitMenuItem.Size = new Size(180, 22);
			exitMenuItem.Text = "E&xit";
			exitMenuItem.Click += exitMenuItem_Click;
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
			aboutMenuItem.Size = new Size(180, 22);
			aboutMenuItem.Text = "&About";
			aboutMenuItem.Click += aboutMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1096, 577);
            Controls.Add(panel1);
            Controls.Add(treeView1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Text = "Database Manager";
            panel1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel3.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }


        #endregion

        private TreeView treeView1;
        private Panel panel1;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private RichTextBox richTextBox1;
        private Panel panel3;
        private Button button1;
        private Button button2;
        private DataGridView dataGridView1;
        private Panel panel4;
        private Label label4;
        private ComboBox cmbConnectionType;
        private Label label3;
        private ComboBox cmbConnections;
        private TextBox txtConnectionString;
        private Label label2;
        private TextBox txtConnectionName;
        private Label label1;
        private Button btnDeleteConnection;
        private Button btnAddConnection;
        private Button btnAddFolder;
         private TextBox txtGroup;
        private Label labelGroup;
        private TreeView treeViewConnections;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem newQueryMenuItem;
        private ToolStripMenuItem executeQueryMenuItem;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem helpMenuItem;
        private ToolStripMenuItem aboutMenuItem;
    }
}
