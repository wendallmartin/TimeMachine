using System;
using System.Windows.Forms;
using System.Windows.Media;
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

        public event WeekDel PrintWeekEvent;

        public event WeekDel PreviewWeekEvent;

        public WpfWeekViewBar(DateTime dateTime, double hoursinweek)
        {
            BrushSelected = Brushes.DimGray;
            BrushUnselected = Brushes.Gray;
            
            _date = dateTime;

            Text = "Week - " + dateTime.Month + "//" +
                   dateTime.Day + "//" + dateTime.Year + "                                                         Hours: " + hoursinweek;

            DeleteEvent += OnDeleteDay;
            SelectedEvent += OnMouseDown;
        }

        private void OnDeleteDay(ViewBar viewBar)
        {
            var sure = MessageBox.Show(@"Time will be deleted permenetly!", @"Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (sure == DialogResult.Yes)
            {
                DeleteWeekEvent?.Invoke(_date);
            }
        }

        private void OnMouseDown(ViewBar view)
        {
            EmailOrPrintWindow emailOrPrintWindow = new EmailOrPrintWindow();

            emailOrPrintWindow.ShowDialog();

            var sure = emailOrPrintWindow.DialogResult;

            if (sure == DialogResult.Yes)
            {
                EmailWeekEvent?.Invoke(_date);
            }
            else if (sure == DialogResult.No)
            {
                PrintWeekEvent?.Invoke(_date);
            }
            else if (sure == DialogResult.OK)
            {
                PreviewWeekEvent?.Invoke(_date);
            }
        }
    }
}
