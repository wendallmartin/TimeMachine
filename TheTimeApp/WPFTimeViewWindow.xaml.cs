using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using static TheTimeApp.Controls.PrevEmailWin;
using Brushes = System.Windows.Media.Brushes;
using Day = TheTimeApp.TimeData.Day;
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

        /// <summary>
        /// Min date used for scroll.
        /// </summary>
        private DateTime _min;

        /// <summary>
        /// Max date used for scroll.
        /// </summary>
        private DateTime _max;

        public WpfTimeViewWindow()
        {
            InitializeComponent();
            
            _24Hour = AppSettings.Instance.MilitaryTime == "true";

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

            _min = DataBaseManager.Instance.MinDate();

            _max = DataBaseManager.Instance.MaxDate();

            _baseDate = DataBaseManager.Instance.CurrentDay().Date;
            
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void InitTimes(List<DateTime> startEnd)
        {
            bool honest = File.Exists("honest.txt");
            StackPanel.Children.Clear();
            double totalHours = TimeServer.DecToQuarter(DataBaseManager.Instance.HoursInRange(startEnd[0], startEnd[1]).TotalHours);
            
            if (DateTimeFormatInfo.CurrentInfo != null)
            {
                WpfWeekViewBar weekViewBar = new WpfWeekViewBar(startEnd[0], totalHours);
                weekViewBar.DeleteWeekEvent += OnDeleteWeek;
                weekViewBar.EmailWeekEvent += HtmlTimeReporter.OnEmailWeek;
                weekViewBar.PreviewWeekEvent += HtmlTimeReporter.OnPreviewWeek;
                StackPanel.Children.Add(weekViewBar);
            }
            
            Day prev = new Day(new DateTime(2001, 1, 1));
            foreach (Day day in DataBaseManager.Instance.DaysInRange(startEnd[0], startEnd[1]))
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
            DataBaseManager.Instance.DeleteRange(TimeServer.StartEndWeek(date)[0], TimeServer.StartEndWeek(date)[1]);
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void OnDeleteDayClick(Day day)
        {
            DataBaseManager.Instance.DeleteDay(day.Date);
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
            DataBaseManager.Instance.DeleteTime(time.Key);
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void TimeViewTimeClick(WpfTimeViewBar viewBar)
        {
            string prevKey = viewBar.GetKey();
            WpfTimeEdit timeEdit = new WpfTimeEdit(viewBar.GetTime());

            var result = timeEdit.ShowDialog();

            if (result.HasValue && result.Value)
            {
                DataBaseManager.Instance.UpdateTime(prevKey, timeEdit.Time);
            }
            
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void Twelve_Hour_Click(object sender, EventArgs e)
        {
            TwelveHourButton.Background = Brushes.LightSkyBlue;
            TwentyFourHourButton.Background = Brushes.Transparent;
            
            TimeFormatExpander.IsExpanded = false;
            AppSettings.Instance.MilitaryTime = "false";
            _24Hour = AppSettings.Instance.MilitaryTime == "true";
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void TwentyFour_Hour_Click(object sender, EventArgs e)
        {
            TwentyFourHourButton.Background = Brushes.LightSkyBlue;
            TwelveHourButton.Background = Brushes.Transparent;
            
            TimeFormatExpander.IsExpanded = false;
            AppSettings.Instance.MilitaryTime = "true";
            _24Hour = AppSettings.Instance.MilitaryTime == "true";
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void Btn_NextClick(object sender, RoutedEventArgs e)
        {
            _baseDate = new DateTime(Math.Min(_baseDate.AddDays(7).Ticks, _max.Date.Ticks));
            
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void Btn_PrevClick(object sender, RoutedEventArgs e)
        {
            _baseDate = new DateTime(Math.Max(_baseDate.AddDays(-7).Ticks, _min.Ticks));
            InitTimes(TimeServer.StartEndWeek(_baseDate));
        }

        private void BtnTotalTimeClick(object sender, RoutedEventArgs e)
        {
            PrevEmailWin emailOrPreview = new PrevEmailWin();

            emailOrPreview.ShowDialog();

            ResultValue result = emailOrPreview.Result;

            switch (result)
            {
                case ResultValue.Email:
                    HtmlTimeReporter.OnEmailWeek(_baseDate);
                    break;
                case ResultValue.Prev:
                    HtmlTimeReporter.OnPreviewWeek(_baseDate);
                    break;
            }
        }
    }
}