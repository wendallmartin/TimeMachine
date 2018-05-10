namespace TheTimeApp
{
    partial class EmailOrPrintWindow
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
            this.btn_Email = new System.Windows.Forms.Button();
            this.btn_Print = new System.Windows.Forms.Button();
            this.btn_Preview = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Email
            // 
            this.btn_Email.Location = new System.Drawing.Point(12, 21);
            this.btn_Email.Name = "btn_Email";
            this.btn_Email.Size = new System.Drawing.Size(75, 41);
            this.btn_Email.TabIndex = 1;
            this.btn_Email.Text = "Email";
            this.btn_Email.UseVisualStyleBackColor = true;
            this.btn_Email.Click += new System.EventHandler(this.btn_Email_Click);
            // 
            // btn_Print
            // 
            this.btn_Print.Location = new System.Drawing.Point(102, 21);
            this.btn_Print.Name = "btn_Print";
            this.btn_Print.Size = new System.Drawing.Size(75, 41);
            this.btn_Print.TabIndex = 2;
            this.btn_Print.Text = "Print";
            this.btn_Print.UseVisualStyleBackColor = true;
            this.btn_Print.Click += new System.EventHandler(this.btn_Print_Click);
            // 
            // btn_Preview
            // 
            this.btn_Preview.Location = new System.Drawing.Point(196, 21);
            this.btn_Preview.Name = "btn_Preview";
            this.btn_Preview.Size = new System.Drawing.Size(75, 41);
            this.btn_Preview.TabIndex = 3;
            this.btn_Preview.Text = "Preview";
            this.btn_Preview.UseVisualStyleBackColor = true;
            this.btn_Preview.Click += new System.EventHandler(this.btn_Preview_Click);
            // 
            // EmailOrPrintWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 74);
            this.Controls.Add(this.btn_Preview);
            this.Controls.Add(this.btn_Print);
            this.Controls.Add(this.btn_Email);
            this.Name = "EmailOrPrintWindow";
            this.Text = "EmailOrPrintWindow";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_Email;
        private System.Windows.Forms.Button btn_Print;
        private System.Windows.Forms.Button btn_Preview;
    }
}