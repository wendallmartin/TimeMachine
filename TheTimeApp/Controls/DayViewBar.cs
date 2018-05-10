using System;
using System.Drawing;
using System.Windows.Forms;

namespace TheTimeApp
{
    public class DayViewBar : Panel
    {
        private Label label;
        private DateTime date;
        private Button delete;

        public delegate void DayDelegate(DateTime date);

        public event DayDelegate DeleteDayEvent;

        public event DayDelegate MouseClickEvent;

        public DayViewBar(Size size, TimeData.Day day)
        {
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseClick += OnDayClick;
            BackColor = Color.Green;
            Size = size;
            date = day.Date;

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

            label = new Label { AutoSize = true, Text = date.Month + "//" + date.Day + "//" + date.Year + "                                              Hours: " 
                + day.HoursAsDec(), Location = new Point(10, 8) };
            label.MouseClick += OnDayClick;
            Controls.Add(label);
            Controls.Add(delete);
        }

        private void OnDayClick(object sender, MouseEventArgs e)
        {
            MouseClickEvent?.Invoke(date);
        }

        private void OnDeleteDay(object sender, MouseEventArgs e)
        {
            var sure = MessageBox.Show("Time will be deleted permenetly!", "Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (sure == DialogResult.Yes)
            {
                DeleteDayEvent?.Invoke(date);
            }
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (!ClientRectangle.Contains(PointToClient(Control.MousePosition)))
            {
                BackColor = Color.Green;
                delete.Hide();   
            }
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            BackColor = Color.LightGreen;
            delete.Show();
        }

        public DateTime Date
        {
            get{ return date; }
        }

    }
}