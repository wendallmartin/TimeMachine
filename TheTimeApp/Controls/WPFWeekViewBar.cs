using System;
using System.Windows.Forms;
using System.Windows.Media;
using TheTimeApp.TimeData;
using static TheTimeApp.Controls.PrevEmailWin;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TheTimeApp.Controls
{
    /// <inheritdoc />
    /// <summary>
    /// The week bar. Inherits ViewBar.
    /// </summary>
    public class WpfWeekViewBar : ViewBar 
    {
        private readonly DateTime _date;

        public delegate void WeekDel(DateTime date);

        public event WeekDel DeleteWeekEvent;

        public event WeekDel EmailWeekEvent;

        public event WeekDel PreviewWeekEvent;

        public WpfWeekViewBar(DateTime dateTime, double hours)
        {
            BrushSelected = Brushes.DimGray;
            BrushUnselected = Brushes.Gray;
            
            _date = dateTime;

            Text = "Week - " + dateTime.Month + "//" +
                   dateTime.Day + "//" + dateTime.Year + "                                                         Hours: " + $"{TimeServer.DecToQuarter(hours)}";

            DeleteEvent += OnDeleteDay;
            SelectedEvent += OnMouseDown;
        }

        private void OnDeleteDay(ViewBar viewBar)
        {
            var sure = MessageBox.Show(@"Time will be deleted permanently!", @"Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (sure == DialogResult.Yes)
            {
                DeleteWeekEvent?.Invoke(_date);
            }
        }

        private void OnMouseDown(ViewBar view)
        {
            PrevEmailWin emailOrPreview = new PrevEmailWin();

            emailOrPreview.ShowDialog();

            ResultValue result = emailOrPreview.Result;

            if (result == ResultValue.Email)
            {
                EmailWeekEvent?.Invoke(_date);
            }
            else if (result == ResultValue.Prev)
            {
                PreviewWeekEvent?.Invoke(_date);
            }
        }
    }
}
