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

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow(_timeData);
            settings.ShowDialog();
        }
    }
}
