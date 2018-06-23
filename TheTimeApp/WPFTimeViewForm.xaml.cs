using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Size = System.Windows.Size;
//To the top of my class file:
using Forms = System.Windows.Forms;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for WPFTimeViewForm.xaml
    /// </summary>
    public partial class WPFTimeViewForm : Window
    {
        public TimeData.TimeData _timeData;
        private bool _12hour = true;

        public WPFTimeViewForm()
        {
            InitializeComponent();

            AppSettings.Validate();
           
            if (AppSettings.SQLEnabled == "true" && SqlServerHelper.IsConnected)
            {
                _timeData = new TimeData.TimeData(true);
                _timeData.LoadDataFromSqlSever();
                ConnectionChanged(true);
                UpdateChanged(true);
            }
            else
            {
                _timeData = new TimeData.TimeData(false);
                _timeData = TimeData.TimeData.Load();
            }
            
            InitualizeView();

            _timeData.TimeDataUpdated += OnTimeDataUpdate;
            _timeData.ConnectionChangedEvent += ConnectionChanged;
            _timeData.UpdateChangedEvent += UpdateChanged;
        }

        /// <summary>
        /// Saves time to file and reinitualized the dispaly.
        /// </summary>
        /// <param name="data"></param>
        private void OnTimeDataUpdate(TimeData.TimeData data)
        {
            Debug.WriteLine("Intualize view");
            _timeData.Days = data.Days; // just copy the days to avoid having to reassociate all events
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
            foreach (Day day in _timeData.Days)
            {
                if (!DatesAreInTheSameWeek(day.Date, prev.Date))
                {
                    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
                    var d2 = day.Date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(day.Date) + 1);
                    WpfWeekViewBar weekViewBar = new WpfWeekViewBar( d2, _timeData.HoursInWeek(d2)){Editable = false};
                    weekViewBar.EmailWeekEvent += OnEmailWeek;
                    weekViewBar.PrintWeekEvent += OnPrintWeek;
                    weekViewBar.PreviewWeekEvent += OnPreviewWeek;
                    StackPanel.Children.Add(weekViewBar);
                }
                WpfDayViewBar datevViewBar = new WpfDayViewBar(day){Editable = false};
                datevViewBar.DayClickEvent += OnDateViewDayClick;
                StackPanel.Children.Add(datevViewBar);

                prev = day;

                if (day == _timeData.Days[_timeData.Days.Count - 1])
                {
                    ScrollViewer.ScrollToBottom();
                }
            }
        }

        private void OnDateViewDayClick(DateTime date)
        {
            Day day = _timeData.Days.First(d => d.Date == date);
            if (day == null) return;

            WpfDayViewEdit dayView = new WpfDayViewEdit(_timeData, day){Enabled = false};
            dayView.ShowDialog();
        }

        private void OnPreviewWeek(DateTime date)
        {
            PrintDocument p = new PrintDocument();
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(_timeData.ConverWeekToText(date), new Font("Times New Roman", 12), new SolidBrush(System.Drawing.Color.Black),
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
                e1.Graphics.DrawString(_timeData.ConverWeekToText(date), new Font("Times New Roman", 12), new SolidBrush(System.Drawing.Color.Black),
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
                    msg.Body = _timeData.ConverWeekToText(date);
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
            SettingsWindow settings = new SettingsWindow(_timeData);
            settings.ShowDialog();
        }
    }
}
