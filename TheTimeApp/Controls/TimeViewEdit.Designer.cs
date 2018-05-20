using System.Windows.Forms;

namespace TheTimeApp
{
    partial class TimeViewEdit
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
            this.button1 = new System.Windows.Forms.Button();
            this.lb_PunchIn = new System.Windows.Forms.Label();
            this.lb_Punchout = new System.Windows.Forms.Label();
            this.timeInPicker = new System.Windows.Forms.DateTimePicker();
            this.timeOutPicker = new System.Windows.Forms.DateTimePicker();
            this.bt_Save = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.SystemColors.GrayText;
            this.button1.Location = new System.Drawing.Point(248, 0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(19, 24);
            this.button1.TabIndex = 3;
            this.button1.Text = "X";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.XButtonClick);
            // 
            // lb_PunchIn
            // 
            this.lb_PunchIn.AutoSize = true;
            this.lb_PunchIn.Location = new System.Drawing.Point(47, 15);
            this.lb_PunchIn.Name = "lb_PunchIn";
            this.lb_PunchIn.Size = new System.Drawing.Size(49, 13);
            this.lb_PunchIn.TabIndex = 4;
            this.lb_PunchIn.Text = "Punch in";
            // 
            // lb_Punchout
            // 
            this.lb_Punchout.AutoSize = true;
            this.lb_Punchout.Location = new System.Drawing.Point(162, 15);
            this.lb_Punchout.Name = "lb_Punchout";
            this.lb_Punchout.Size = new System.Drawing.Size(56, 13);
            this.lb_Punchout.TabIndex = 5;
            this.lb_Punchout.Text = "Punch out";
            // 
            // timeInPicker
            // 
            this.timeInPicker.CustomFormat = "hh:mm";
            this.timeInPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.timeInPicker.Location = new System.Drawing.Point(36, 41);
            this.timeInPicker.Name = "timeInPicker";
            this.timeInPicker.Size = new System.Drawing.Size(75, 20);
            this.timeInPicker.TabIndex = 17;
            // 
            // timeOutPicker
            // 
            this.timeOutPicker.CustomFormat = "hh:mm";
            this.timeOutPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.timeOutPicker.Location = new System.Drawing.Point(152, 41);
            this.timeOutPicker.Name = "timeOutPicker";
            this.timeOutPicker.Size = new System.Drawing.Size(75, 20);
            this.timeOutPicker.TabIndex = 18;
            // 
            // bt_Save
            // 
            this.bt_Save.Location = new System.Drawing.Point(93, 75);
            this.bt_Save.Name = "bt_Save";
            this.bt_Save.Size = new System.Drawing.Size(75, 23);
            this.bt_Save.TabIndex = 19;
            this.bt_Save.Text = "Save";
            this.bt_Save.UseVisualStyleBackColor = true;
            this.bt_Save.Click += new System.EventHandler(this.bt_Save_Click);
            // 
            // TimeViewEdit
            // 
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(270, 102);
            this.Controls.Add(this.bt_Save);
            this.Controls.Add(this.timeOutPicker);
            this.Controls.Add(this.timeInPicker);
            this.Controls.Add(this.lb_Punchout);
            this.Controls.Add(this.lb_PunchIn);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "TimeViewEdit";
            this.Text = "TimeViewEdit";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label lb_PunchIn;
        private System.Windows.Forms.Label lb_Punchout;
        private System.Windows.Forms.DateTimePicker timeInPicker;
        private DateTimePicker timeOutPicker;
        private Button bt_Save;
    }
}