using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Windows;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using Brushes = System.Windows.Media.Brushes;
using DataBase = TheTimeApp.TimeData.TimeData;
//To the top of my class file:
using Forms = System.Windows.Forms;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for WPFTimeViewForm.xaml
    /// </summary>
    public partial class WPFTimeViewForm
    {
        public WPFTimeViewForm()
        {
            InitializeComponent();

            lb_VersionNumber.Content = UpDater.CurrentVersion;

            AppSettings.Validate();
           
            DataBase.Load();
            
            if (AppSettings.SQLEnabled == "true" && SqlServerHelper.IsConnected)
            {
                DataBase.TimeDataBase.LoadCurrentUserFromSql();
                ConnectionChanged(true);
                UpdateChanged(true);
            }

            InitualizeView();

            AssociateSqlEvents();
        }

        private void AssociateSqlEvents()
        {
            DataBase.TimeDataBase.TimeDataUpdated += OnTimeDataUpdate;
            DataBase.TimeDataBase.ConnectionChangedEvent += ConnectionChanged;
            DataBase.TimeDataBase.UpdateChangedEvent += UpdateChanged;
        }
        
        private void UnAssociateSqlEvents()
        {
            DataBase.TimeDataBase.TimeDataUpdated = null;
            DataBase.TimeDataBase.ConnectionChangedEvent = null;
            DataBase.TimeDataBase.UpdateChangedEvent = null;
        }

        private void LoadUsers()
        {
            pnl_UserSelection.Children.Clear();
            foreach (User user in DataBase.TimeDataBase.Users)
            {
                ViewBar userBar = new ViewBar()
                {
                    BrushUnselected = Brushes.DarkGray, 
                    BrushSelected = Brushes.DimGray, 
                    Text = user.UserName, 
                    Width = 120, 
                    Height = 26, 
                    Editable = false
                };
                userBar.SelectedEvent += OnUserSelected;
                pnl_UserSelection.Children.Add(userBar);
            }
        }

        private void OnUserSelected(ViewBar view)
        {
            btn_SelectedUser.Content = DataBase.TimeDataBase.CurrentUserName;
            scroll_UserSelection.Visibility = Visibility.Hidden;
            
            UnAssociateSqlEvents();
            bool connectedAndEnabled = AppSettings.SQLEnabled == "true" && SqlServerHelper.IsConnected; 
            if (connectedAndEnabled)
            {
                DataBase.TimeDataBase.CurrentUserName = view.Text;
                DataBase.TimeDataBase.LoadCurrentUserFromSql();
                DataBase.TimeDataBase.Save();

            }
            ConnectionChanged(connectedAndEnabled);
            UpdateChanged(connectedAndEnabled);            
            
            AssociateSqlEvents();
            InitualizeView();
        }

        private void btn_SelectedUser_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
            scroll_UserSelection.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Saves time to file and reinitualized the dispaly.
        /// </summary>
        /// <param name="days"></param>
        private void OnTimeDataUpdate(List<Day> days)
        {
            Debug.WriteLine("Intualize view");
            DataBase.TimeDataBase.Days = days; // just copy the days to avoid having to reassociate all events
            Dispatcher.Invoke(InitualizeView);
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
            foreach (Day day in DataBase.TimeDataBase.Days)
            {
                if (!DatesAreInTheSameWeek(day.Date, prev.Date))
                {
                    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
                    var d2 = day.Date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(day.Date) + 1);
                    WpfWeekViewBar weekViewBar = new WpfWeekViewBar( d2, DataBase.TimeDataBase.HoursInWeek(d2)){Editable = false};
                    weekViewBar.EmailWeekEvent += OnEmailWeek;
                    weekViewBar.PrintWeekEvent += OnPrintWeek;
                    weekViewBar.PreviewWeekEvent += OnPreviewWeek;
                    StackPanel.Children.Add(weekViewBar);
                }
                WpfDayViewBar datevViewBar = new WpfDayViewBar(day){Editable = false};
                datevViewBar.DayClickEvent += OnDateViewDayClick;
                StackPanel.Children.Add(datevViewBar);

                prev = day;

                if (day == DataBase.TimeDataBase.Days[DataBase.TimeDataBase.Days.Count - 1])
                {
                    ScrollViewer.ScrollToBottom();
                }
            }

            btn_SelectedUser.Content = DataBase.TimeDataBase.CurrentUserName;
        }

        private void OnDateViewDayClick(DateTime date)
        {
            Day day = DataBase.TimeDataBase.Days.First(d => d.Date == date);
            if (day == null) return;

            WpfDayViewEdit dayView = new WpfDayViewEdit(day){Enabled = false};
            dayView.ShowDialog();
        }

        private void OnPreviewWeek(DateTime date)
        {
            PrintDocument p = new PrintDocument();
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(DataBase.TimeDataBase.ConverWeekToText(date), new Font("Times New Roman", 12), new SolidBrush(System.Drawing.Color.Black),
                    new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));

            };
            try
            {
                Forms.PrintPreviewDialog pdp = new Forms.PrintPreviewDialog();
                pdp.Document = p;

                if (pdp.ShowDialog() == Forms.DialogResult.OK)
                {
                    p.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void OnPrintWeek(DateTime date)
        {
            PrintDocument p = new PrintDocument();
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(DataBase.TimeDataBase.ConverWeekToText(date), new Font("Times New Roman", 12), new SolidBrush(System.Drawing.Color.Black),
                    new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));

            };
            try
            {
                Forms.PrintDialog pdp = new Forms.PrintDialog();
                pdp.Document = p;

                if (pdp.ShowDialog() == Forms.DialogResult.OK)
                {
                    p.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void OnEmailWeek(DateTime date)
        {
            new Thread(() =>
            {
                try
                {
                    MailMessage msg = new MailMessage(AppSettings.FromAddress, AppSettings.ToAddress);
                    SmtpClient smtp = new SmtpClient();
                    NetworkCredential basicCredential = new NetworkCredential(AppSettings.FromUser, AppSettings.FromPass);
                    smtp.EnableSsl = AppSettings.SslEmail == "true";
                    smtp.Port = Convert.ToInt32(AppSettings.FromPort);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = basicCredential;
                    smtp.Host = AppSettings.EmailHost;
                    msg.Subject = "Time";
                    msg.Body = DataBase.TimeDataBase.ConverWeekToText(date);
                    smtp.Send(msg);
                    MessageBox.Show("Mail sent!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }).Start();
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.ShowDialog();
        }
    }
}
