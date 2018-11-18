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
        private readonly Day _day;

        public delegate void DayDelegate(Day day);

        public event DayDelegate DeleteDayEvent;

        public event DayDelegate DayClickEvent;

        public WpfDayViewBar(Day day)
        {
            BrushSelected = Brushes.LightSkyBlue;
            BrushUnselected = Brushes.CadetBlue;
            
            _day = day;
            
            Text = _day.Date.Month + "//" + _day.Date.Day + "//" + _day.Date.Year + "                                              Hours: " + $"{day.HoursAsDecToQuarter}";

            DeleteEvent += OnDeleteDay;
            SelectedEvent += OnDayDayClick;
            Width = 280;
        }

        private void OnDayDayClick(ViewBar view)
        {
            DayClickEvent?.Invoke(_day);
        }

        private void OnDeleteDay(ViewBar viewBar)
        {
            MessageBoxButton button = MessageBoxButton.YesNo;

            if (MessageBox.Show("Time will be deleted permanently!", "Warning", button) == (MessageBoxResult) DialogResult.Yes)
            {
                DeleteDayEvent?.Invoke(_day);
            }
        }
    }
}
