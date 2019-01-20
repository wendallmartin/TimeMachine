using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LibGit2Sharp;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;

namespace TheTimeApp
{

    /// <summary>
    /// Interaction logic for WPFTimeAppForm.xaml
    /// </summary>
    public partial class WPFTimeAppForm
    {
        private System.Timers.Timer _timeTic;
        private System.Timers.Timer _detailsChanged;

        private Task _gitTask;
        
        public WPFTimeAppForm()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            lb_VersionNumber.Content = Program.CurrentVersion;

            _detailsChanged = new System.Timers.Timer() {Interval = 2000};
            _detailsChanged.AutoReset = false;

            _timeTic = new System.Timers.Timer() {Interval = 60000};
            _timeTic.Elapsed += TimeTic;
            _timeTic.AutoReset = true;

            DetailsCommitView.Date = DataBaseManager.Instance.CurrentDay().Date;
            DetailsCommitView.DayDetails = DataBaseManager.Instance.CurrentDay().Details;
            DetailsCommitView.Init();
            btn_SelectedUser.Content = TimeServer.SqlCurrentUser;
            
            if (AppSettings.Instance.SqlEnabled != "true")
            {
                SqlStatusBar.Visibility = Visibility.Hidden;
                Start_Button.Margin = new Thickness(10,20,10,10);
                Start_Button.Height = 115;
            }
            else
            {
                SqlStatusBar.Visibility = Visibility.Visible;
                Start_Button.Margin = new Thickness(10,0,10,40);
                Start_Button.Height = 85;
            }

            DataBaseManager.Instance.ConnectionChangedEvent += ConnectionChanged;
            DataBaseManager.Instance.UpdateChangedEvent += SqlUpdateChanged;

            SetStartChecked();
            
            UpdateTime();
            UpdateGitIfEnabled();
        }

        private void LoadUsers()
        {
            pnl_UserSelection.Children.Clear();
            foreach (string name in DataBaseManager.Instance.UserNames())
            {
                ViewBar userBar = new ViewBar()
                {
                    BrushUnselected = Brushes.DarkGray, 
                    BrushSelected = Brushes.DimGray, 
                    Text = name, 
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
            TimeServer.SqlCurrentUser = view.Text;
            btn_SelectedUser.Content = TimeServer.SqlCurrentUser;
            AppSettings.Instance.CurrentUser = TimeServer.SqlCurrentUser;
            scroll_UserSelection.Visibility = Visibility.Hidden;
            DataBaseManager.Instance.VerifySql();
            DetailsCommitView.DayDetails = DataBaseManager.Instance.CurrentDay().Details;
            UpdateTime();
        }

        private void btn_SelectedUser_Click(object sender, EventArgs e)
        {
            LoadUsers();
            scroll_UserSelection.Visibility = scroll_UserSelection.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void SetStartChecked()
        {
            if (DataBaseManager.Instance.IsClockedIn())
            {
                _timeTic.Start();
                Start_Button.Background = Brushes.Red;
                Start_Button.Content = "Stop";
            }
            else
            {
                _timeTic.Stop();
                Start_Button.Background = Brushes.Green;
                Start_Button.Content = "Start";
            }
        }

        private void btn_Start_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (Equals(Start_Button.Background, Brushes.Green))
            {
                ReLoadDay();
                UpdateTime();
                DataBaseManager.Instance.PunchIn(TimeServer.GenerateId());// unique 15 digit 
            }
            else
            {
                DataBaseManager.Instance.PunchOut(DataBaseManager.Instance.LastTimeId());
                UpdateTime();
                UpdateGitIfEnabled();
            }
            
            SetStartChecked();
        }

        /// <summary>
        /// Load current day details and comments into
        /// details/comment view. This fixed details
        /// cary over previous day issue.
        /// </summary>
        private void ReLoadDay()
        {
            DetailsCommitView.Date = DataBaseManager.Instance.CurrentDay().Date;
            DetailsCommitView.DayDetails = DataBaseManager.Instance.CurrentDay().Details;
        }

        /// <summary>
        /// Event adapter for timer _timTic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeTic(object sender, ElapsedEventArgs e)
        {
            UpdateTime();
            UpdateGitIfEnabled();
        }

        private void UpdateGitIfEnabled()
        {
            if (!AppSettings.Instance.GitEnabled) return;
            if (_gitTask != null && _gitTask.Status == TaskStatus.Running) return;
            
            _gitTask = Task.Run(() =>
            {
                var newCommits = GitManager.Instance.CommitsOnDate(DateTime.Today);
                var oldCommits = DataBaseManager.Instance.GetCommits();

                foreach (GitCommit gitCommit in newCommits)
                {
                    // Git branch must be specified
                    if (string.IsNullOrEmpty(gitCommit.Branch)) continue;
                    if (string.IsNullOrWhiteSpace(gitCommit.Branch)) continue;
                    if (gitCommit.Branch.Contains("(no branch)")) continue;
                    
                    // Branch must be unique
                    if (oldCommits.Any(c => c.Message == gitCommit.Message)) continue;

                    
                    DataBaseManager.Instance.AddCommit(gitCommit);
                }
            
                DetailsCommitView.LoadCommitMsgs();       
            });
        }

        private void UpdateTime()
        {
            TimeSpan ts = DataBaseManager.Instance.HoursInRange(DateTime.Today, DateTime.Today);
            if (DataBaseManager.Instance.IsClockedIn())
            {
                Day day = DataBaseManager.Instance.CurrentDay();
                if (day.Times.Count > 0)
                {
                    DateTime lasTimeIn = day.Times.Last().TimeIn;
                    TimeSpan additional = DateTime.Now - lasTimeIn;
                    ts = ts.Add(additional);    
                }
            }

            Dispatcher.Invoke(() => { Lbl_Time.Content = $"{ts.Hours:0}:{ts.Minutes:00}"; });
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

        private void DetailsCommitView_OnDetailsChangedEvent(string newDetails)
        {
            if (AppSettings.Instance.SqlEnabled == "true")
            {
                if(_detailsChanged.Enabled)// Stop any previous timers
                    _detailsChanged.Stop();
            
                _detailsChanged.Start();    
            }
            DataBaseManager.Instance.UpdateDetails(DataBaseManager.Instance.CurrentDay().Date, newDetails);
        }
        
        private void btn_Report_Click(object sender, RoutedEventArgs e)
        {
            new WpfTimeViewWindow().ShowDialog();
            Init();
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
            Init();// reinitialize
        }
    }
}
