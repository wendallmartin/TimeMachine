using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Day = TheTimeApp.TimeData.Day;
using MessageBox = System.Windows.MessageBox;

namespace TheTimeApp.Controls
{
    /// <inheritdoc />
    /// <summary>
    /// The day bar. Inherites ViewBar.
    /// </summary>
    public class WpfDayViewBar : ViewBar
    {
        private readonly DateTime _date;

        public delegate void DayDelegate(DateTime date);

        public event DayDelegate DeleteDayEvent;

        public event DayDelegate DayClickEvent;

        public WpfDayViewBar(Day day)
        {
            BrushSelected = Brushes.LightSkyBlue;
            BrushUnselected = Brushes.CadetBlue;
            
            _date = day.Date;
            
            Text = _date.Month + "//" + _date.Day + "//" + _date.Year + "                                              Hours: " + day.HoursAsDec();

            DeleteEvent += OnDeleteDay;
            SelectedEvent += OnDayDayClick;
        }

        private void OnDayDayClick()
        {
            DayClickEvent?.Invoke(_date);
        }

        private void OnDeleteDay(ViewBar viewBar)
        {
            MessageBoxButton button = MessageBoxButton.YesNo;

            if (MessageBox.Show("Time will be deleted permenetly!", "Warning", button) == (MessageBoxResult) DialogResult.Yes)
            {
                DeleteDayEvent?.Invoke(_date);
            }
        }
    }
}
