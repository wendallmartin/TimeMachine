using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Windows;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using Brushes = System.Windows.Media.Brushes;
using DataBase = TheTimeApp.TimeData.TimeData;
using DateTime = System.DateTime;
//To the top of my class file:
using Forms = System.Windows.Forms;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for WPFTimeViewForm.xaml
    /// </summary>
    public partial class WPFTimeViewForm
    {
        private DateTime _currentDate; 
        public WPFTimeViewForm()
        {
            InitializeComponent();

            _currentDate = DateTime.Now;

            lb_VersionNumber.Content = Program.CurrentVersion;

            DataBaseManager.Initualize();
           
            if (AppSettings.Instance.SqlEnabled == "true")
            {
                DataBaseManager.Instance.PullSecondaryToPrimary();
                DataBaseManager.Instance.ConnectionChangedEvent += ConnectionChanged;
            }

            InitualizeView();

        }
        
        private void LoadUsers()
        {
            pnl_UserSelection.Children.Clear();
            foreach (string user in DataBaseManager.Instance.UserNames())
            {
                ViewBar userBar = new ViewBar()
                {
                    BrushUnselected = Brushes.DarkGray, 
                    BrushSelected = Brushes.DimGray, 
                    Text = user, 
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
            scroll_UserSelection.Visibility = Visibility.Hidden;
            
            bool connectedAndEnabled = AppSettings.Instance.SqlEnabled == "true"; 
            if (connectedAndEnabled)
            {
                DataBaseManager.Instance.PullSecondaryToPrimary();
            }
            
            InitualizeView();
        }

        private void btn_SelectedUser_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
            scroll_UserSelection.Visibility = Visibility.Visible;
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

        private void InitualizeView()
        {
            StackPanel.Children.Clear();
            
            var a = TimeServer.StartEndWeek(_currentDate)[0];
            var b = TimeServer.StartEndWeek(_currentDate)[1];

            double hoursInWeek = DataBaseManager.Instance.HoursInRange(a, b).TotalHours;
            WpfWeekViewBar weekViewBar = new WpfWeekViewBar( a, TimeServer.DecToQuarter(hoursInWeek)){Deletable = false};
            weekViewBar.EmailWeekEvent += HtmlTimeReporter.OnEmailWeek;
            weekViewBar.PreviewWeekEvent += HtmlTimeReporter.OnPreviewWeek;
            StackPanel.Children.Add(weekViewBar);
            
            foreach (Day day in DataBaseManager.Instance.DaysInRange(a,b))
            {
                WpfDayViewBar datevViewBar = new WpfDayViewBar(day){Deletable = false};
                datevViewBar.DayClickEvent += OnDateViewDayClick;
                StackPanel.Children.Add(datevViewBar);
            }
            
            ScrollViewer.ScrollToBottom();
            btn_SelectedUser.Content = TimeServer.SqlCurrentUser;
        }

        private void OnDateViewDayClick(Day day)
        {
            if (day == null) return;

            WpfDayViewEdit dayView = new WpfDayViewEdit(day){Enabled = false};
            dayView.ShowDialog();
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.ShowDialog();
        }
    }
}
