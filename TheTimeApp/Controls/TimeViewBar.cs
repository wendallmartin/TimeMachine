using System;
using System.Drawing;
using System.Windows.Forms;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;

namespace TheTimeApp
{
    public partial class TimeView : Panel
    {
        private Time _time;

        TimeViewEdit timeedit;

        public delegate void DeleteDel(Time time);

        public event DeleteDel DeleteEvent;

        public delegate void SelectedDel(TimeView view);

        public event SelectedDel SelectedEvent;

        private Button delete;

        private bool _is12hour;
        public TimeView(Time time, bool is12hour = true)
        {
            InitializeComponent();

            _time = time;
            _is12hour = is12hour;

            delete = new Button
            {
                Text = "X",
                Size = new Size(20, Height - 4),
                Location = new Point(Width - 22, 2),
                Visible = false,
            };
            delete.MouseEnter += OnMouseEnter;

            InitualizeView();
        }

        private void InitualizeView()
        {
            Controls.Clear();
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseClick += OnMouseClick;

            Label timeLabel = new Label
            {
                Location = new Point(10, 6),
                AutoSize = true
            };


            if (_is12hour)
            {
                timeLabel.Text = "        In: " + _time.TimeIn.ToString("hh:mm tt") + "   Out: " + _time.TimeOut.ToString("hh:mm tt")
                                 + "   Total: " + _time.GetTime().Hours + ":" + _time.GetTime().Minutes;
            }
            else
            {
                timeLabel.Text = "        In: " + _time.TimeIn.ToString("HH:mm") + "   Out: " + _time.TimeOut.ToString("HH:mm") 
                                 + "   Total: " + _time.GetTime().Hours + ":" + _time.GetTime().Minutes;
            }


            timeLabel.MouseEnter += OnMouseEnter;
            timeLabel.MouseLeave += OnMouseLeave;
            timeLabel.MouseClick += OnMouseClick;


            delete.MouseClick += DeleteTime;

            Controls.Add(delete);
            Controls.Add(timeLabel);
        }

        private void DeleteTime(object sender, MouseEventArgs e)
        {
            var sure = MessageBox.Show("Time will be deleted permenetly!", "Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (sure == DialogResult.Yes)
            {
                DeleteEvent?.Invoke(_time);
            }
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            SelectedEvent?.Invoke(this);
        }

        public Time GetTime()
        {
            return _time;
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            delete.Show();
            BackColor = Color.CornflowerBlue;
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (!ClientRectangle.Contains(PointToClient(Control.MousePosition)))
            {
                delete.Hide();
                BackColor = Color.LightSlateGray;
            }
        }

        public void UnSelect()
        {
            delete.Hide();
            BackColor = Color.LightSlateGray;
        }
    }
}
