using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;

namespace TheTimeApp
{

    /// <summary>
    /// Interaction logic for WPFTimeAppForm.xaml
    /// </summary>
    public partial class WPFTimeAppForm
    {
        private System.Timers.Timer _detailsChanged;
        
        public WPFTimeAppForm()
        {
            InitializeComponent();
            lb_VersionNumber.Content = Program.CurrentVersion;
            AppSettings.Validate();

            TimeData.TimeData.Load();
            TimeData.TimeData.TimeDataBase.Save();
            TimeData.TimeData.TimeDataBase.ConnectionChangedEvent += ConnectionChanged;
            TimeData.TimeData.TimeDataBase.SqlUpdateChanged += SqlUpdateChanged;
            
            if (AppSettings.SQLEnabled == "true")
            {
                TimeData.TimeData.TimeDataBase.SetUpSqlServer();
                SqlUpdateChanged(TimeData.TimeData.TimeDataBase.SerilizableCommands);
            }
            
            SetStartChecked();
            
            _detailsChanged = new System.Timers.Timer(){Interval = 2000};
            _detailsChanged.Elapsed += OnDetailsChangeTick;
            _detailsChanged.AutoReset = false;
            
            DayDetailsBox.Text = TimeData.TimeData.TimeDataBase.CurrentDay().Details;

            btn_SelectedUser.Content = TimeData.TimeData.TimeDataBase.CurrentUserName;
        }

        private void LoadUsers()
        {
            pnl_UserSelection.Children.Clear();
            foreach (User user in TimeData.TimeData.TimeDataBase.Users)
            {
                ViewBar userBar = new ViewBar()
                {
                    BrushUnselected = Brushes.DarkGray, 
                    BrushSelected = Brushes.DimGray, 
                    Text = user.UserName, 
                    Width = 120, 
                    Height = 26, 
                    Deletable = false
                };
                userBar.SelectedEvent += OnUserSelected;
                pnl_UserSelection.Children.Add(userBar);
            }
        }

        private void OnUserSelected(ViewBar view)
        {
            TimeData.TimeData.TimeDataBase.CurrentUserName = view.Text;
            TimeData.TimeData.TimeDataBase.Save();
            btn_SelectedUser.Content = TimeData.TimeData.TimeDataBase.CurrentUserName;
            scroll_UserSelection.Visibility = Visibility.Hidden;
        }

        private void btn_SelectedUser_Click(object sender, EventArgs e)
        {
            LoadUsers();
            scroll_UserSelection.Visibility = scroll_UserSelection.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void SetStartChecked()
        {
            if (TimeData.TimeData.TimeDataBase.ClockedIn())
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
                TimeData.TimeData.TimeDataBase.PunchIn();
                Start_Button.Background = Brushes.Red;
                Start_Button.Content = "Stop";
            }
            else
            {
                TimeData.TimeData.TimeDataBase.PunchOut();
                Start_Button.Background = Brushes.Green;
                Start_Button.Content = "Start";
            }
        }

        private void SqlUpdateChanged(List<SerilizeSqlCommand> value)
        {
            new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (value == null || value.Count == 0)
                    {
                        lbl_UpToDate.Content = "Up to date";
                        lbl_UpToDate.Foreground = Brushes.Green;
                    }
                    else
                    {
                        lbl_UpToDate.Content = $"{value.Count} Not up to date ";
                        lbl_UpToDate.Foreground = Brushes.Red;
                    }
                });
            }).Start();
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

        private void OnDayDetailsChanged(object sender, TextChangedEventArgs e)
        {
            TimeData.TimeData.TimeDataBase.Save();
            
            if (AppSettings.SQLEnabled == "true")
            {
                if(_detailsChanged.Enabled)// Stop any previous timers
                    _detailsChanged.Stop();
            
                _detailsChanged.Start();    
            }
        }
        
        private void OnDetailsChangeTick(object sender, ElapsedEventArgs e)
        {
            string details = "";
            Dispatcher.Invoke(() => { details = DayDetailsBox.Text; });
            TimeData.TimeData.TimeDataBase.UpdateDetails(TimeData.TimeData.TimeDataBase.CurrentDay(), details);
        }

        private void btn_Report_Click(object sender, RoutedEventArgs e)
        {
            new WpfTimeViewWindow().ShowDialog();
            DayDetailsBox.Text = TimeData.TimeData.TimeDataBase.CurrentDay().Details;
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow esettings = new SettingsWindow();
            esettings.ShowDialog();
            
            TimeData.TimeData.Load();
            SetStartChecked();
            DayDetailsBox.Text = TimeData.TimeData.TimeDataBase.CurrentDay().Details;
        }
    }
}
