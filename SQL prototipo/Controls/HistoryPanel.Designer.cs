namespace SQL_prototipo.Controls
{
    partial class HistoryPanel
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            _listBoxHistory = new ListBox();
            _historyLabel = new Label();
            SuspendLayout();
            // 
            // _listBoxHistory
            // 
            _listBoxHistory.Dock = DockStyle.Fill;
            _listBoxHistory.DrawMode = DrawMode.OwnerDrawVariable;
            _listBoxHistory.HorizontalScrollbar = false;
            _listBoxHistory.IntegralHeight = false;
            _listBoxHistory.Name = "_listBoxHistory";
            _listBoxHistory.DrawItem += ListBoxHistory_DrawItem;
            _listBoxHistory.MeasureItem += ListBoxHistory_MeasureItem;
            _listBoxHistory.DoubleClick += ListBoxHistory_DoubleClick;
            // 
            // _historyLabel
            // 
            _historyLabel.AutoSize = false;
            _historyLabel.Dock = DockStyle.Top;
            _historyLabel.Height = 20;
            _historyLabel.Name = "_historyLabel";
            _historyLabel.Text = "Query History (double-click to load)";
            _historyLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // HistoryPanel
            // 
            Controls.Add(_listBoxHistory);
            Controls.Add(_historyLabel);
            Name = "HistoryPanel";
            ResumeLayout(false);
        }

        #endregion

        private ListBox _listBoxHistory;
        private Label _historyLabel;
    }
}
