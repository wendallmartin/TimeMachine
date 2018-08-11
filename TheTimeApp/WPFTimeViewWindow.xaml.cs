﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Windows;
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
        private DateTime _baseDate;

        TimeViewEdit _timeedit;

        public WpfTimeViewWindow()
        {
            InitializeComponent();
            
            _24Hour = AppSettings.MilitaryTime == "true";

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

            _baseDate = LocalSql.Instance.CurrentDay().Date;
            
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void InitTimes(List<DateTime> startEnd)
        {
            bool honest = File.Exists("honest.txt");
            StackPanel.Children.Clear();
            double totalHours = Math.Round(LocalSql.Instance.HoursInRange(startEnd[0], startEnd[1]), 1);
            
            if (DateTimeFormatInfo.CurrentInfo != null)
            {
                WpfWeekViewBar weekViewBar = new WpfWeekViewBar(startEnd[0], totalHours);
                weekViewBar.DeleteWeekEvent += OnDeleteWeek;
                weekViewBar.EmailWeekEvent += OnEmailWeek;
                weekViewBar.PrintWeekEvent += OnPrintWeek;
                weekViewBar.PreviewWeekEvent += OnPreviewWeek;
                StackPanel.Children.Add(weekViewBar);
            }
            
            Day prev = new Day(new DateTime(2001, 1, 1));
            foreach (Day day in LocalSql.Instance.DaysInRange(startEnd[0], startEnd[1]))
            {
                WpfDayViewBar datevViewBar = new WpfDayViewBar(day);
                datevViewBar.DayClickEvent += OnDateViewClick;
                datevViewBar.DeleteDayEvent += OnDeleteDayClick;
                StackPanel.Children.Add(datevViewBar);
                foreach (Time time in day.Times)
                {
                    WpfTimeViewBar timeView = new WpfTimeViewBar(time, _24Hour){ReadOnly = !honest};
                    timeView.TimeDeleteEvent += TimeDeleteTime;
                    timeView.TimeClickEvent += TimeViewTimeClick;
                    StackPanel.Children.Add(timeView);
                }

                prev = day;
            }

            TotalTime.Content = totalHours;
        }

        private void OnDeleteWeek(DateTime date)
        {
            LocalSql.Instance.DeleteRange(TimeServer.StartEndWeek(date)[0], TimeServer.StartEndWeek(date)[1]);
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void OnDeleteDayClick(Day day)
        {
            LocalSql.Instance.DeleteDay(day.Date);
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void OnDateViewClick(Day day)
        {
            if (day == null) return;

            WpfDayViewEdit dayView = new WpfDayViewEdit(day);
            dayView.ShowDialog();

            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void TimeDeleteTime(Time time)
        {
            LocalSql.Instance.DeleteTime(time.Key);
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void TimeViewTimeClick(WpfTimeViewBar view)
        {
            Time prevTime = new Time {TimeIn = view.GetTime().TimeIn, TimeOut = view.GetTime().TimeOut, Key = view.GetKey()};
            _timeedit = new TimeViewEdit(view.GetTime(), _24Hour);

            if (_timeedit.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocalSql.Instance.UpdateTime(prevTime.Key, _timeedit.GetTime);
            }

            _timeedit.Close();
            InitTimes(TimeServer.StartEndWeek(_baseDate));
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
                    msg.Body = LocalSql.Instance.GetRangeAsText(TimeServer.StartEndWeek(date)[0], TimeServer.StartEndWeek(date)[1]);
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
                e1.Graphics.DrawString(LocalSql.Instance.GetRangeAsText(TimeServer.StartEndWeek(date)[0], TimeServer.StartEndWeek(date)[1]), new Font("Times New Roman", 12), new SolidBrush(Color.Black),
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
                e1.Graphics.DrawString(LocalSql.Instance.GetRangeAsText(TimeServer.StartEndWeek(date)[0], TimeServer.StartEndWeek(date)[1]), new Font("Times New Roman", 12), new SolidBrush(Color.Black),
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
            _24Hour = AppSettings.MilitaryTime == "true";
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void TwentyFour_Hour_Click(object sender, EventArgs e)
        {
            TwentyFourHourButton.Background = Brushes.LightSkyBlue;
            TwelveHourButton.Background = Brushes.Transparent;
            
            TimeFormatExpander.IsExpanded = false;
            AppSettings.MilitaryTime = "true";
            _24Hour = AppSettings.MilitaryTime == "true";
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void Btn_NextClick(object sender, System.Windows.RoutedEventArgs e)
        {
            _baseDate = new DateTime(Math.Min(_baseDate.AddDays(7).Ticks, LocalSql.Instance.MaxDate().Date.Ticks));
            
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void Btn_PrevClick(object sender, RoutedEventArgs e)
        {
            _baseDate = new DateTime(Math.Max(_baseDate.AddDays(-7).Ticks, LocalSql.Instance.MinDate().Date.Ticks));
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void BtnTotalTimeClick(object sender, RoutedEventArgs e)
        {
            EmailOrPrintWindow emailOrPrintWindow = new EmailOrPrintWindow();

            emailOrPrintWindow.ShowDialog();

            DialogResult sure = emailOrPrintWindow.DialogResult;

            if (sure == System.Windows.Forms.DialogResult.Yes)
            {
                OnEmailWeek(_baseDate);
            }
            else if (sure == System.Windows.Forms.DialogResult.No)
            {
                OnPrintWeek(_baseDate);
            }
            else if (sure == System.Windows.Forms.DialogResult.OK)
            {
                OnPreviewWeek(_baseDate);
            }           
        }
    }
}