using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TheTimeApp
{

    /// <summary>
    /// Interaction logic for WPFTimeAppForm.xaml
    /// </summary>
    public partial class WPFTimeAppForm : Window
    {
        private TimeData.TimeData _timeData = new TimeData.TimeData();
        private DateTime startTime;
        private DateTime StopTime;
        private DateTime _hours;

        private TimeViewWindow timeViewWindow;

        public delegate void Exit();

        public event Exit CloseApp;

        public WPFTimeAppForm()
        {
            InitializeComponent();

            AppSettings.Validate();

            _timeData = TimeData.TimeData.Load();

            DayDetailsBox.Text = _timeData.CurrentDay().Details;
        }

        private void btn_Start_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (Start_Button.Background == Brushes.Green)
            {
                _timeData.PunchIn();
                Start_Button.Background = Brushes.Red;
                Start_Button.Content = "Stop";
            }
            else
            {
                _timeData.PunchOut();
                Start_Button.Background = Brushes.Green;
                Start_Button.Content = "Start";
            }
        }

        private void Report_Click(object sender, RoutedEventArgs e)
        {
            _timeData.SortDays();
            timeViewWindow = new TimeViewWindow(_timeData);
            timeViewWindow.CloseEvent += CloseTimeViewWindow;
            timeViewWindow.ShowDialog();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow esettings = new SettingsWindow(_timeData);
            esettings.ShowDialog();
        }

        private void CloseTimeViewWindow(TimeData.TimeData data)
        {
            _timeData = data;
            _timeData.Save();
            timeViewWindow.Close();

            DayDetailsBox.Text = _timeData.CurrentDay().Details;
        }

        private void OnDayDetailsChanged(object sender, TextChangedEventArgs e)
        {
            _timeData.UpdateDetails(_timeData.CurrentDay(), DayDetailsBox.Text);
            _timeData.Save();
        }
    }
}
