using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using Control = System.Windows.Controls.Control;
using MessageBox = System.Windows.MessageBox;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for WPFTimeViewWindow.xaml
    /// </summary>
    public partial class WPFTimeViewWindow : Window
    {
        private TimeData.TimeData _timeData;

        private int _lastScrollPos = 0;

        private bool _12hour = true;

        TimeViewEdit timeedit;

        private List<WPFTimeViewBar> timeViews;

        public WPFTimeViewWindow(TimeData.TimeData timeData)
        {
            InitializeComponent();
            _timeData = timeData;

            _12hour = AppSettings.MilitaryTime != "true";
            InitTimes(true);
        }

        private bool DatesAreInTheSameWeek(DateTime date1, DateTime date2)
        {
            var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
            var d1 = date1.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date1));
            var d2 = date2.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date2));

            return d1 == d2;
        }

        private void InitTimes(bool scrolltoend = false)
        {
            timeViews = new List<WPFTimeViewBar>();

            StackPanel.Children.Clear();
            TimeData.Day prev = new TimeData.Day(new DateTime(2001,1,1));
            foreach (TimeData.Day day in _timeData.Days)
            {
                if (!DatesAreInTheSameWeek(day.Date, prev.Date))
                {
                    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
                    var d2 = day.Date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(day.Date) + 1);
                    WPFWeekViewBar weekViewBar = new WPFWeekViewBar(d2, _timeData.HoursInWeek(d2));
                    weekViewBar.DeleteWeekEvent += OnDeleteWeek;
                    weekViewBar.EmailWeekEvent += OnEmailWeek;
                    weekViewBar.PrintWeekEvent += OnPrintWeek;
                    weekViewBar.PreviewWeekEvent += OnPreviewWeek;
                    StackPanel.Children.Add(weekViewBar);
                }
                WPFDayViewBar datevViewBar = new WPFDayViewBar(day);
                datevViewBar.DayClickEvent += OnDateViewClick;
                datevViewBar.DeleteDayEvent += OnDeleteDayClick;
                StackPanel.Children.Add((Control) datevViewBar);

                foreach (Time time in day.Times)
                {
                    WPFTimeViewBar timeView = new WPFTimeViewBar(time, _12hour);
                    timeView.TimeDeleteEvent += TimeDeleteTime;
                    timeView.TimeClickEvent += TimeViewTimeClick;
                    StackPanel.Children.Add(timeView);
                    timeViews.Add(timeView);
                }
                prev = day;
            }
            if(scrolltoend)
                ScrollViewer.ScrollToBottom();
        }

        private void OnDeleteWeek(DateTime date)
        {
            _timeData.DeleteWeek(date);
            InitTimes();
        }

        private void OnDeleteDayClick(DateTime date)
        {
            _timeData.DeleteDay(date);
            InitTimes();
        }

        private void OnDateViewClick(DateTime date)
        {
            TimeData.Day day = _timeData.Days.First(d => d.Date == date);
            if (day == null) return;

            WpfDayViewEdit dayView = new WpfDayViewEdit(_timeData, day);
            dayView.ShowDialog();

            _timeData.Save();

            InitTimes();
        }

        private void TimeDeleteTime(Time time)
        {
            _timeData.DeleteTime(time);
            InitTimes();
        }

        private void TimeViewTimeClick(WPFTimeViewBar view)
        {
            int dayIndex = -1;
            int timeIndex = -1;
            for (int i = 0; i < _timeData.Days.Count; i++)
            {
                if (_timeData.Days[i].Contains(view.GetTime()))
                {
                    dayIndex = i;
                    for (int j = 0; j < _timeData.Days[i].Times.Count; j++)
                    {
                        if (_timeData.Days[i].Times[j] == view.GetTime())
                        {
                            timeIndex = j;
                        }
                    }
                    break;
                }
            }
            if (dayIndex == -1 || timeIndex == -1)
            {
                MessageBox.Show("Invalid operation!");
            }

            timeedit = new TimeViewEdit(view.GetTime(), _timeData, _12hour);

            var result = timeedit.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                _timeData.UpdateDayDate(dayIndex, timeedit.GetDate);
                _timeData.UpdateDayTime(new KeyValuePair<int, int>(dayIndex, timeIndex), timeedit.GetTime);
            }
            timeedit.Close();
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
                PrintDialog pdp = new PrintDialog();
                pdp.Document = p;

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
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(_timeData.ConverWeekToText(date), new Font("Times New Roman", 12), new SolidBrush(System.Drawing.Color.Black),
                    new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));

            };
            try
            {
                PrintPreviewDialog pdp = new PrintPreviewDialog();
                pdp.Document = p;

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

        private void hToolStripMenuItem12hour_Click(object sender, EventArgs e)
        {
            AppSettings.MilitaryTime = "false";
            _12hour = true;
            InitTimes();
        }

        private void hToolStripMenuItem24hour_Click(object sender, EventArgs e)
        {
            AppSettings.MilitaryTime = "true";
            _12hour = false;
            InitTimes();
        }
    }
}
