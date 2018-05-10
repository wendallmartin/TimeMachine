using System;
using System.Drawing;
using System.Windows.Forms;

namespace TheTimeApp.Controls
{
    public class WeekViewBar : Panel
    {
        private Label seperaterLabel;
        private DateTime date;
        private Button delete;

        public delegate void WeekDel(DateTime date);

        public event WeekDel DeleteWeekEvent;

        public event WeekDel EmailWeekEvent;

        public event WeekDel PrintWeekEvent;

        public event WeekDel PreviewWeekEvent;

        public WeekViewBar(Size size, DateTime dateTime, double hoursinweek)
        {
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseDown += OnMouseDown;

            BackColor = Color.Gray;
            Size = size;
            date = dateTime;
            
            delete = new Button
            {
                Text = "X",
                Size = new Size(20, Height - 4),
                Location = new Point(Width - 22, 2),
                Visible = false,
            };
            delete.MouseEnter += OnMouseEnter;
            delete.MouseLeave += OnMouseLeave;
            delete.MouseClick += OnDeleteDay;

            seperaterLabel = new Label {AutoSize = true, Text = "Week - " + dateTime.Month  + "//" + 
                dateTime.Day  + "//" + dateTime.Year + "                                                         Hours: " + hoursinweek, Location = new Point(10, 8)};

            seperaterLabel.MouseDown += OnMouseDown;

            Controls.Add(seperaterLabel);
            Controls.Add(delete);
        }

        private void OnDeleteDay(object sender, MouseEventArgs e)
        {
            var sure = MessageBox.Show("Time will be deleted permenetly!", "Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (sure == DialogResult.Yes)
            {
                DeleteWeekEvent?.Invoke(date);
            }
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (!ClientRectangle.Contains(PointToClient(Control.MousePosition)))
            {
                BackColor = Color.Gray;
                delete.Hide();   
            }
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            BackColor = Color.LightGray;
            delete.Show();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                EmailOrPrintWindow emailOrPrintWindow = new EmailOrPrintWindow();

                emailOrPrintWindow.ShowDialog();

                var sure = emailOrPrintWindow.DialogResult;

                if (sure == DialogResult.Yes)
                {
                    EmailWeekEvent?.Invoke(date);
                }
                else if(sure == DialogResult.No)
                {
                    PrintWeekEvent?.Invoke(date);
                }
                else if(sure == DialogResult.OK)
                {
                    PreviewWeekEvent?.Invoke(date);
                }
            }
        }

        public DateTime Date
        {
            get{ return date; }
        }
    }
}