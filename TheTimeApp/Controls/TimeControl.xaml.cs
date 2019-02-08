using System;
using System.Windows;
using TheTimeApp.TimeData;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for DateTimeControl.xaml
    /// </summary>
    public partial class TimeControl
    {
        public TimeControl()
        {
            InitializeComponent();
        }
        
        public static readonly DependencyProperty DateTimeProperty =
            DependencyProperty.Register("DateTime", typeof(DateTime), typeof(TimeControl), new UIPropertyMetadata(DateTime.Now, OnDateTimeChanged));
        
        public static readonly DependencyProperty HoursProperty = 
            DependencyProperty.Register("Hours", typeof(string), typeof(TimeControl), new UIPropertyMetadata("00", OnTimeChanged));

        public static readonly DependencyProperty MinutesProperty =
            DependencyProperty.Register("Minutes", typeof(string), typeof(TimeControl), new UIPropertyMetadata("00", OnTimeChanged));

        public static readonly DependencyProperty AmPmStringProperty = 
            DependencyProperty.Register("AmPmString", typeof(string), typeof(TimeControl), new UIPropertyMetadata("AM", OnTimeChanged));
        
        public string AmPmString
        {
            get => (string) GetValue(AmPmStringProperty);
            set => SetValue(AmPmStringProperty, value);
        }

        public DateTime DateTime
        {
            get => (DateTime)GetValue(DateTimeProperty);
            set => SetValue(DateTimeProperty, value);
        }
        
        public string Hours
        {
            get => (string)GetValue(HoursProperty);
            set => SetValue(HoursProperty, value);
        }
        

        public string Minutes
        {
            get => (string)GetValue(MinutesProperty);
            set => SetValue(MinutesProperty, value);
        } 
        
        /// <summary>
        /// Update control with new date-time.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnDateTimeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is TimeControl control)) return;
            
            control.Hours = ((DateTime)e.NewValue).ToString("hh");
            control.Minutes = ((DateTime)e.NewValue).ToString("mm");
            control.AmPmString = ((DateTime)e.NewValue).ToString("tt");
        }

        /// <summary>
        /// Update control with date-time values.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnTimeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is TimeControl control)) return;

            control.DateTime = DateTime.Parse($"{control.DateTime.Year}-{control.DateTime.Month}-{control.DateTime.Day} {control.Hours}:{control.Minutes} {control.AmPmString}"); 
        }

        private void Btn_AmPm_Click(object sender, RoutedEventArgs e)
        {
            AmPmString = AmPmString == "AM" ? "PM" : "AM";
        }
    }
}
