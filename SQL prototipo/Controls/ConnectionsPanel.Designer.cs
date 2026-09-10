namespace SQL_prototipo.Controls
{
    partial class ConnectionsPanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            treeViewConnections = new TreeView();
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
            flButtons = new FlowLayoutPanel();
            btnAddConnection = new Button();
            btnAddFolder = new Button();
            btnDeleteConnection = new Button();
            btnTestConnection = new Button();
            btnUpdateConnection = new Button();
            panel4.SuspendLayout();
            flButtons.SuspendLayout();
            SuspendLayout();
            // 
            // treeViewConnections
            // 
            treeViewConnections.Dock = DockStyle.Fill;
            treeViewConnections.HideSelection = false;
            treeViewConnections.Location = new Point(0, 0);
            treeViewConnections.Name = "treeViewConnections";
            treeViewConnections.Size = new Size(247, 690);
            treeViewConnections.TabIndex = 0;
            treeViewConnections.AfterSelect += TreeViewConnections_AfterSelect;
            treeViewConnections.NodeMouseDoubleClick += TreeViewConnections_NodeMouseDoubleClick;
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
            panel4.Controls.Add(flButtons);
            panel4.Dock = DockStyle.Right;
            panel4.Location = new Point(247, 0);
            panel4.Name = "panel4";
            panel4.Size = new Size(719, 690);
            panel4.TabIndex = 1;
            // 
            // labelGroup
            // 
            labelGroup.AutoSize = true;
            labelGroup.Location = new Point(10, 148);
            labelGroup.Name = "labelGroup";
            labelGroup.Size = new Size(40, 15);
            labelGroup.TabIndex = 11;
            labelGroup.Text = "Group";
            // 
            // txtGroup
            // 
            txtGroup.Location = new Point(10, 168);
            txtGroup.Name = "txtGroup";
            txtGroup.PlaceholderText = "e.g., Development, Production";
            txtGroup.Size = new Size(341, 23);
            txtGroup.TabIndex = 10;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(10, 198);
            label4.Name = "label4";
            label4.Size = new Size(83, 15);
            label4.TabIndex = 9;
            label4.Text = "Database Type";
            // 
            // cmbConnectionType
            // 
            cmbConnectionType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbConnectionType.FormattingEnabled = true;
            cmbConnectionType.Items.AddRange(new object[] { "SqlServer", "LocalDB" });
            cmbConnectionType.Location = new Point(10, 218);
            cmbConnectionType.Name = "cmbConnectionType";
            cmbConnectionType.Size = new Size(341, 23);
            cmbConnectionType.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(10, 248);
            label3.Name = "label3";
            label3.Size = new Size(107, 15);
            label3.TabIndex = 7;
            label3.Text = "Switch Connection";
            // 
            // cmbConnections
            // 
            cmbConnections.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbConnections.FormattingEnabled = true;
            cmbConnections.Location = new Point(10, 268);
            cmbConnections.Name = "cmbConnections";
            cmbConnections.Size = new Size(341, 23);
            cmbConnections.TabIndex = 6;
            cmbConnections.SelectedIndexChanged += CmbConnections_SelectedIndexChanged;
            // 
            // txtConnectionString
            // 
            txtConnectionString.Location = new Point(10, 70);
            txtConnectionString.Multiline = true;
            txtConnectionString.Name = "txtConnectionString";
            txtConnectionString.PlaceholderText = "e.g., Server=(localdb)\\MSSQLLocalDB;Integrated Security=true";
            txtConnectionString.Size = new Size(341, 75);
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
            // flButtons
            // 
            flButtons.Controls.Add(btnAddConnection);
            flButtons.Controls.Add(btnAddFolder);
            flButtons.Controls.Add(btnDeleteConnection);
            flButtons.Controls.Add(btnTestConnection);
            flButtons.Controls.Add(btnUpdateConnection);
            flButtons.Dock = DockStyle.Right;
            flButtons.FlowDirection = FlowDirection.TopDown;
            flButtons.Location = new Point(370, 0);
            flButtons.Name = "flButtons";
            flButtons.Size = new Size(349, 690);
            flButtons.TabIndex = 12;
            flButtons.WrapContents = false;
            // 
            // btnAddConnection
            // 
            btnAddConnection.Location = new Point(10, 5);
            btnAddConnection.Margin = new Padding(10, 5, 10, 5);
            btnAddConnection.Name = "btnAddConnection";
            btnAddConnection.Size = new Size(320, 40);
            btnAddConnection.TabIndex = 0;
            btnAddConnection.Text = "Add Connection";
            btnAddConnection.UseVisualStyleBackColor = true;
            btnAddConnection.Click += BtnAddConnection_Click;
            // 
            // btnAddFolder
            // 
            btnAddFolder.Location = new Point(10, 55);
            btnAddFolder.Margin = new Padding(10, 5, 10, 5);
            btnAddFolder.Name = "btnAddFolder";
            btnAddFolder.Size = new Size(320, 40);
            btnAddFolder.TabIndex = 2;
            btnAddFolder.Text = "Add Folder";
            btnAddFolder.UseVisualStyleBackColor = true;
            btnAddFolder.Click += BtnAddFolder_Click;
            // 
            // btnDeleteConnection
            // 
            btnDeleteConnection.Location = new Point(10, 105);
            btnDeleteConnection.Margin = new Padding(10, 5, 10, 5);
            btnDeleteConnection.Name = "btnDeleteConnection";
            btnDeleteConnection.Size = new Size(320, 40);
            btnDeleteConnection.TabIndex = 1;
            btnDeleteConnection.Text = "Delete Connection";
            btnDeleteConnection.UseVisualStyleBackColor = true;
            btnDeleteConnection.Click += BtnDeleteConnection_Click;
            // 
            // btnTestConnection
            // 
            btnTestConnection.Location = new Point(10, 155);
            btnTestConnection.Margin = new Padding(10, 5, 10, 5);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(327, 37);
            btnTestConnection.TabIndex = 0;
            btnTestConnection.Text = "Test Connection";
            btnTestConnection.UseVisualStyleBackColor = true;
            btnTestConnection.Click += BtnTestConnection_Click;
            // 
            // btnUpdateConnection
            // 
            btnUpdateConnection.Location = new Point(10, 202);
            btnUpdateConnection.Margin = new Padding(10, 5, 10, 5);
            btnUpdateConnection.Name = "btnUpdateConnection";
            btnUpdateConnection.Size = new Size(327, 48);
            btnUpdateConnection.TabIndex = 0;
            btnUpdateConnection.Text = "Update Connection";
            btnUpdateConnection.UseVisualStyleBackColor = true;
            btnUpdateConnection.Click += BtnUpdateConnection_Click;
            // 
            // ConnectionsPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(treeViewConnections);
            Controls.Add(panel4);
            Name = "ConnectionsPanel";
            Size = new Size(966, 690);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            flButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TreeView treeViewConnections;
        private Panel panel4;
        private FlowLayoutPanel flButtons;
        private Button btnTestConnection;
        private Button btnUpdateConnection;
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
    }
}
