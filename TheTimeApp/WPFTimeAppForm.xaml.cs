using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            _timeData.ConnectionChangedEvent += ConnectionChanged;
            _timeData.UpdateChangedEvent += UpdateChanged;
            
            SetStartChecked();
            
            DayDetailsBox.Text = _timeData.CurrentDay().Details;
        }

        private void SetStartChecked()
        {
            if (_timeData.ClockedIn())
            {
                Start_Button.Background = Brushes.Red;
                Start_Button.Content = "Stop";
            }
            else
            {
                Start_Button.Background = Brushes.Green;
                Start_Button.Content = "Start";
            }
        }

        private void btn_Start_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (Equals(Start_Button.Background, Brushes.Green))
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
        
        
        private void UpdateChanged(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                if (connected)
                {
                    lbl_UpToDate.Content = "Up to date";
                    lbl_UpToDate.Foreground = Brushes.Green;
                }
                else
                {
                    lbl_UpToDate.Content = "NOT up to date";
                    lbl_UpToDate.Foreground = Brushes.Red;
                }
            });

        }

        private void ConnectionChanged(bool con)
        {
            Dispatcher.Invoke(() =>
            {
                if (con)
                {
                    lbl_Connected.Content = "Connected";
                    lbl_Connected.Foreground = Brushes.Green;
                }
                else
                {
                    lbl_Connected.Content = "Disconnected";
                    lbl_Connected.Foreground = Brushes.Red;
                }
            });
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

        private void btn_Report_Click(object sender, RoutedEventArgs e)
        {
            _timeData.SortDays();
            timeViewWindow = new TimeViewWindow(_timeData);
            timeViewWindow.CloseEvent += CloseTimeViewWindow;
            timeViewWindow.ShowDialog();
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow esettings = new SettingsWindow(_timeData);
            esettings.ShowDialog();
            
            _timeData = TimeData.TimeData.Load();
            SetStartChecked();
            DayDetailsBox.Text = _timeData.CurrentDay().Details;
        }
    }
}
