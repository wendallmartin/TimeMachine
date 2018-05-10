namespace TheTimeApp
{
    partial class TimeViewWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimeViewWindow));
            this.ViewToolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.timeFormatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hToolStripMenuItem12hour = new System.Windows.Forms.ToolStripMenuItem();
            this.hToolStripMenuItem24hour = new System.Windows.Forms.ToolStripMenuItem();
            this.TimeLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.Xbutton = new System.Windows.Forms.Button();
            this.ViewToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // ViewToolStrip
            // 
            this.ViewToolStrip.AllowItemReorder = true;
            this.ViewToolStrip.BackColor = System.Drawing.SystemColors.GrayText;
            this.ViewToolStrip.CanOverflow = false;
            this.ViewToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDropDownButton1});
            this.ViewToolStrip.Location = new System.Drawing.Point(0, 0);
            this.ViewToolStrip.Name = "ViewToolStrip";
            this.ViewToolStrip.Padding = new System.Windows.Forms.Padding(0);
            this.ViewToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.ViewToolStrip.Size = new System.Drawing.Size(438, 25);
            this.ViewToolStrip.TabIndex = 0;
            this.ViewToolStrip.Text = "toolStrip1";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.timeFormatToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(90, 22);
            this.toolStripDropDownButton1.Text = "View Settings";
            // 
            // timeFormatToolStripMenuItem
            // 
            this.timeFormatToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hToolStripMenuItem12hour,
            this.hToolStripMenuItem24hour});
            this.timeFormatToolStripMenuItem.Name = "timeFormatToolStripMenuItem";
            this.timeFormatToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            this.timeFormatToolStripMenuItem.Text = "Time Format";
            // 
            // hToolStripMenuItem12hour
            // 
            this.hToolStripMenuItem12hour.Name = "hToolStripMenuItem12hour";
            this.hToolStripMenuItem12hour.Size = new System.Drawing.Size(93, 22);
            this.hToolStripMenuItem12hour.Text = "12h";
            this.hToolStripMenuItem12hour.Click += new System.EventHandler(this.hToolStripMenuItem12hour_Click);
            // 
            // hToolStripMenuItem24hour
            // 
            this.hToolStripMenuItem24hour.Name = "hToolStripMenuItem24hour";
            this.hToolStripMenuItem24hour.Size = new System.Drawing.Size(93, 22);
            this.hToolStripMenuItem24hour.Text = "24h";
            this.hToolStripMenuItem24hour.Click += new System.EventHandler(this.hToolStripMenuItem24hour_Click);
            // 
            // TimeLayoutPanel
            // 
            this.TimeLayoutPanel.AutoScroll = true;
            this.TimeLayoutPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.TimeLayoutPanel.Location = new System.Drawing.Point(4, 29);
            this.TimeLayoutPanel.Name = "TimeLayoutPanel";
            this.TimeLayoutPanel.Size = new System.Drawing.Size(430, 467);
            this.TimeLayoutPanel.TabIndex = 1;
            // 
            // Xbutton
            // 
            this.Xbutton.BackColor = System.Drawing.SystemColors.GrayText;
            this.Xbutton.Location = new System.Drawing.Point(418, 0);
            this.Xbutton.Name = "Xbutton";
            this.Xbutton.Size = new System.Drawing.Size(19, 24);
            this.Xbutton.TabIndex = 2;
            this.Xbutton.Text = "X";
            this.Xbutton.UseVisualStyleBackColor = false;
            this.Xbutton.Click += new System.EventHandler(this.XButton_Click);
            // 
            // TimeViewWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(438, 500);
            this.Controls.Add(this.Xbutton);
            this.Controls.Add(this.TimeLayoutPanel);
            this.Controls.Add(this.ViewToolStrip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "TimeViewWindow";
            this.Text = "TimeView";
            this.ViewToolStrip.ResumeLayout(false);
            this.ViewToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip ViewToolStrip;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem timeFormatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hToolStripMenuItem12hour;
        private System.Windows.Forms.ToolStripMenuItem hToolStripMenuItem24hour;
        private System.Windows.Forms.FlowLayoutPanel TimeLayoutPanel;
        private System.Windows.Forms.Button Xbutton;
    }
}