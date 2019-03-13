using System;
using System.Windows;
using TheTimeApp.TimeData;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for WpfTimeEdit.xaml
    /// </summary>
    public partial class WpfTimeEdit
    {
        public Time Time { get; }
        
        public WpfTimeEdit(Time time)
        {
            InitializeComponent();

            Time = time;

            InDatePicker.SelectedDate = InTime.DateTime = time.TimeIn;
            OutDatePicker.SelectedDate = OutTime.DateTime = time.TimeOut;
        }

        private void Btn_SaveClick(object sender, RoutedEventArgs e)
        {
            if (InDatePicker.SelectedDate.HasValue && OutDatePicker.SelectedDate.HasValue)
            {
                int inYear = InDatePicker.SelectedDate.Value.Year;
                int inMonth = InDatePicker.SelectedDate.Value.Month;
                int inDay = InDatePicker.SelectedDate.Value.Day;
                int inHour = InTime.DateTime.Hour;
                int inMin = InTime.DateTime.Minute;
                int inSec = InTime.DateTime.Second;
                
                int outYear = OutDatePicker.SelectedDate.Value.Year;
                int outMonth = OutDatePicker.SelectedDate.Value.Month;
                int outDay = OutDatePicker.SelectedDate.Value.Day;
                int outHour = OutTime.DateTime.Hour;
                int outMin = OutTime.DateTime.Minute;
                int outSec = OutTime.DateTime.Second;
            
                DateTime timeIn = new DateTime(inYear, inMonth, inDay, inHour, inMin, inSec);
                DateTime timeOut = new DateTime(outYear, outMonth, outDay, outHour, outMin, outSec);
                
                Time.TimeIn = timeIn;
                Time.TimeOut = timeOut;    
            }

            DialogResult = true;
            Close();
        }
    }
}
