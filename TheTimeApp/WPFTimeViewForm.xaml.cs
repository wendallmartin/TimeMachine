using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for WPFTimeViewForm.xaml
    /// </summary>
    public partial class WPFTimeViewForm : Window
    {
        TimeData.TimeData _timeData = new TimeData.TimeData();

        public WPFTimeViewForm()
        {
            InitializeComponent();

            AppSettings.Validate();

            _timeData = TimeData.TimeData.Load();
            _timeData.ConnectionChangedEvent += ConnectionChanged;
            _timeData.UpdateChangedEvent += UpdateChanged;
            
            InitualizeView();
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

        private void ConnectionChanged(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                if (connected)
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
        
        private bool DatesAreInTheSameWeek(DateTime date1, DateTime date2)
        {
            var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
            var d1 = date1.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date1));
            var d2 = date2.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date2));

            return d1 == d2;
        }

        private void InitualizeView()
        {
            StackPanel.Children.Clear();
            Day prev = new Day(new DateTime(2001,1,1));
            foreach (Day day in _timeData.Days)
            {
                if (!DatesAreInTheSameWeek(day.Date, prev.Date))
                {
                    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
                    var d2 = day.Date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(day.Date) + 1);
                    WPFWeekViewBar weekViewBar = new WPFWeekViewBar(new Size(Width - 31, 26), d2, _timeData.HoursInWeek(d2)){Editable = false};
                    weekViewBar.EmailWeekEvent += OnEmailWeek;
                    weekViewBar.PrintWeekEvent += OnPrintWeek;
                    weekViewBar.PreviewWeekEvent += OnPreviewWeek;
                    StackPanel.Children.Add(weekViewBar);
                }
                WPFDayViewBar datevViewBar = new WPFDayViewBar(new Size(Width - 31, 26), day){Editable = false};
                datevViewBar.MouseClickEvent += OnDateViewClick;
                StackPanel.Children.Add(datevViewBar);

                prev = day;

                if (day == _timeData.Days[_timeData.Days.Count - 1])
                {
                    ScrollViewer.ScrollToBottom();
                }
            }
        }

        private void OnDateViewClick(DateTime date)
        {
            throw new NotImplementedException();
        }

        private void OnPreviewWeek(DateTime date)
        {
            throw new NotImplementedException();
        }

        private void OnPrintWeek(DateTime date)
        {
            throw new NotImplementedException();
        }

        private void OnEmailWeek(DateTime date)
        {
            throw new NotImplementedException();
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow(_timeData);
            settings.ShowDialog();
        }
    }
}
