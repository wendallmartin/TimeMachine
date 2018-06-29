using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Windows.Forms;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using Brushes = System.Windows.Media.Brushes;
using Day = TheTimeApp.TimeData.Day;
using MessageBox = System.Windows.MessageBox;
using DataBase = TheTimeApp.TimeData.TimeData;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for WPFTimeViewWindow.xaml
    /// </summary>
    public partial class WpfTimeViewWindow
    {
        private bool _24Hour;

        TimeViewEdit _timeedit;

        public WpfTimeViewWindow()
        {
            InitializeComponent();
            
            _24Hour = AppSettings.MilitaryTime != "true";

            if (_24Hour)
            {
                TwentyFourHourButton.Background = Brushes.LightSkyBlue;
                TwelveHourButton.Background = Brushes.Transparent;
            }
            else
            {
                TwelveHourButton.Background = Brushes.LightSkyBlue;
                TwentyFourHourButton.Background = Brushes.Transparent;
            }
            
            
            InitTimes(true);
        }

        private bool DatesAreInTheSameWeek(DateTime date1, DateTime date2)
        {
            Calendar cal = DateTimeFormatInfo.CurrentInfo?.Calendar;
            if (cal == null)
                return false;

            var d1 = date1.Date.AddDays(-1 * (int) cal.GetDayOfWeek(date1));
            var d2 = date2.Date.AddDays(-1 * (int) cal.GetDayOfWeek(date2));
            return d1 == d2;
        }

        private void InitTimes(bool scrolltoend = false)
        {
            StackPanel.Children.Clear();
            Day prev = new Day(new DateTime(2001, 1, 1));
            foreach (Day day in DataBase.TimeDataBase.Days)
            {
                if (!DatesAreInTheSameWeek(day.Date, prev.Date))
                {
                    if (DateTimeFormatInfo.CurrentInfo != null)
                    {
                        var cal = DateTimeFormatInfo.CurrentInfo.Calendar;
                        var d2 = day.Date.Date.AddDays(-1 * (int) cal.GetDayOfWeek(day.Date) + 1);
                        WpfWeekViewBar weekViewBar = new WpfWeekViewBar(d2, DataBase.TimeDataBase.HoursInWeek(d2));
                        weekViewBar.Editable = true;
                        weekViewBar.DeleteWeekEvent += OnDeleteWeek;
                        weekViewBar.EmailWeekEvent += OnEmailWeek;
                        weekViewBar.PrintWeekEvent += OnPrintWeek;
                        weekViewBar.PreviewWeekEvent += OnPreviewWeek;
                        StackPanel.Children.Add(weekViewBar);
                    }
                }

                WpfDayViewBar datevViewBar = new WpfDayViewBar(day);
                datevViewBar.Editable = true;
                datevViewBar.DayClickEvent += OnDateViewClick;
                datevViewBar.DeleteDayEvent += OnDeleteDayClick;
                StackPanel.Children.Add(datevViewBar);
                foreach (Time time in day.Times)
                {
                    WpfTimeViewBar timeView = new WpfTimeViewBar(time, _24Hour);
                    timeView.Editable = true;
                    timeView.TimeDeleteEvent += TimeDeleteTime;
                    timeView.TimeClickEvent += TimeViewTimeClick;
                    StackPanel.Children.Add(timeView);
                }

                prev = day;
            }

            if (scrolltoend) ScrollViewer.ScrollToBottom();
        }

        private void OnDeleteWeek(DateTime date)
        {
            DataBase.TimeDataBase.DeleteWeek(date);
            InitTimes();
        }

        private void OnDeleteDayClick(DateTime date)
        {
            DataBase.TimeDataBase.DeleteDay(date);
            InitTimes();
        }

        private void OnDateViewClick(DateTime date)
        {
            Day day = DataBase.TimeDataBase.Days.First(d => d.Date == date);
            if (day == null) return;

            WpfDayViewEdit dayView = new WpfDayViewEdit(day);
            dayView.ShowDialog();

            DataBase.TimeDataBase.Save();

            InitTimes();
        }

        private void TimeDeleteTime(Time time)
        {
            DataBase.TimeDataBase.DeleteTime(time);
            InitTimes();
        }

        private void TimeViewTimeClick(WpfTimeViewBar view)
        {
            Time prevTime = new Time {TimeIn = view.GetTime().TimeIn, TimeOut = view.GetTime().TimeOut};
            _timeedit = new TimeViewEdit(view.GetTime(), _24Hour);

            if (_timeedit.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DataBase.TimeDataBase.UpdateTime(prevTime, _timeedit.GetTime);
            }

            _timeedit.Close();
            InitTimes();
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

        private void OnPrintWeek(DateTime date)
        {
            PrintDocument p = new PrintDocument();
            p.PrintPage += delegate(object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(DataBase.TimeDataBase.ConverWeekToText(date), new Font("Times New Roman", 12), new SolidBrush(Color.Black),
                    new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
            };
            try
            {
                PrintDialog pdp = new PrintDialog {Document = p};

                if (pdp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    p.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void OnPreviewWeek(DateTime date)
        {
            PrintDocument p = new PrintDocument();
            p.PrintPage += delegate(object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(DataBase.TimeDataBase.ConverWeekToText(date), new Font("Times New Roman", 12), new SolidBrush(Color.Black),
                    new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
            };
            try
            {
                PrintPreviewDialog pdp = new PrintPreviewDialog {Document = p};

                if (pdp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    p.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Twelve_Hour_Click(object sender, EventArgs e)
        {
            TwelveHourButton.Background = Brushes.LightSkyBlue;
            TwentyFourHourButton.Background = Brushes.Transparent;
            
            TimeFormatExpander.IsExpanded = false;
            AppSettings.MilitaryTime = "false";
            _24Hour = false;
            InitTimes();
        }

        private void TwentyFour_Hour_Click(object sender, EventArgs e)
        {
            TwentyFourHourButton.Background = Brushes.LightSkyBlue;
            TwelveHourButton.Background = Brushes.Transparent;
            
            TimeFormatExpander.IsExpanded = false;
            AppSettings.MilitaryTime = "true";
            _24Hour = true;
            InitTimes();
        }
    }
}