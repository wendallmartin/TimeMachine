using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using NLog;
using Timer = System.Timers.Timer;

namespace TheTimeApp.TimeData
{
    public abstract class TimeServer
    {
        public static string AppDataDirectory => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TheTimeApp";
        
        public enum State
        {
            Connected,
            Disconnected
        }
        public State ServerState { get; set; }
            
        private Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly object IsConnectedLock = new object();
        private static readonly object SqlServerLock = new object();
        private static readonly object SqlPullLock = new object();
        private static readonly object SqlPushLock = new object();
        
        public static string SqlCurrentUser { get; set; } = string.Empty;
        protected bool _wasConnected;
        protected readonly Timer _connectionRetry = new Timer(1000);
        protected SqlMode SqlMode { get; set; }
        protected List<SerilizeSqlCommand> Commands;


        public int Port { get; set; }
        
        protected DataTable _dataTable = new DataTable();

        public bool SqlEnabled { get; set; } = true;

        #region props


        #endregion
        
        #region Delegates

        public delegate void ProgressChangedDel(float value);
        public delegate void UpdateChangeDel(List<SerilizeSqlCommand> value);
        public delegate void ConnectionChangedDel(bool value);
        public delegate void ProgressFinishDel();
        public delegate void TimeDateUpdatedDel(List<Day> data);

        #endregion

        #region Events

        public TimeDateUpdatedDel TimeDateaUpdate;
        public ProgressChangedDel ProgressChangedEvent;
        public ProgressFinishDel ProgressFinishEvent;
        public ConnectionChangedDel ConnectionChangedEvent;
        public UpdateChangeDel UpdateChangedEvent;

        #endregion
        
        
        
        
        
        public abstract bool IsClockedIn();

        public abstract void AddUser(User user);
        
        public abstract List<string> UserNames();
        
        public abstract void DeleteUser(string username);

        public abstract Day CurrentDay();
        
        public abstract void AddDay(Day day);

        public abstract List<Day> DaysInRange(DateTime a, DateTime b);
        
        public abstract void DeleteDay(DateTime date);
        
        public abstract List<Day> AllDays();

        public abstract TimeSpan HoursInRange(DateTime a, DateTime b);
        
        public abstract void DeleteRange(DateTime start, DateTime end);
        
        public abstract void PunchIn(string key);

        public abstract void PunchOut(string key);
        
        public abstract List<Time> AllTimes();

        public abstract void DeleteTime(string key);
        
        public abstract void UpdateDetails(DateTime date, string details);
        
        public abstract void UpdateTime(string key, Time upd);

        public abstract string LastTimeId();
        
        public abstract List<Time> TimesinRange(DateTime dateA, DateTime dateB);

        public string UserTable => "Users_TimeTable";

        public abstract DateTime MinDate();
        
        public abstract DateTime MaxDate();

        /// <summary>
        /// Pushes given list of days to
        /// sql server.
        /// </summary>
        /// <param name="days"></param>
        public abstract void Push(List<Day> days);
        
        public abstract List<Day> Pull();

        public string TimeTableName => ToTimeTableName(SqlCurrentUser);

        public string DayTableName => ToDayTableName(SqlCurrentUser);

        /// <summary>
        /// Returns user name converted to table name.
        /// </summary>
        /// <returns></returns>
        public string ToTimeTableName(string user)
        {
            return "Time_" + user + "_TimeTable";
        }


        /// <summary>
        /// Returns user name converted to table name.
        /// </summary>
        /// <returns></returns>
        public string ToDayTableName(string user)
        {
            return "Day_" + user + "_TimeTable";
        }

        /// <summary>
        /// Returns datetime in
        /// yy-mm-dd-hh-mm-milli format.
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static string DateTimeSqLite(DateTime datetime)
        {
            string dateTimeFormat = "{0}-{1}-{2} {3}:{4}:{5}.{6}";
            return string.Format(dateTimeFormat, datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second,datetime.Millisecond);
        }

        public static string DateString(DateTime time)
        {
            return $"{time.Month}/{time.Day}/{time.Year}";
        }
        
        /// <summary>
        /// Returns date only in
        /// yy-mm-dd format.
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static string DateSqLite(DateTime datetime)
        {
            string dateFormat = "{0}-{1}-{2}";
            return string.Format(dateFormat, datetime.Year, datetime.Month, datetime.Day);
        }
        
        public static string TimeSqLite(DateTime datetime)
        {
            string timeFormat = "{1}:{2}:{3}.{4}";
            return string.Format(timeFormat, datetime.Hour, datetime.Minute, datetime.Second,datetime.Millisecond);
        }
        
        public static bool DatesAreInTheSameWeek(DateTime date1, DateTime date2)
        {
            Calendar cal = DateTimeFormatInfo.CurrentInfo?.Calendar;
            if (cal == null)
                return false;

            var d1 = date1.Date.AddDays(-1 * (int) cal.GetDayOfWeek(date1));
            var d2 = date2.Date.AddDays(-1 * (int) cal.GetDayOfWeek(date2));
            return d1 == d2;
        }
        
        public static List<DateTime> StartEndWeek(DateTime date)
        {
            List<DateTime> startEnd = new List<DateTime>();
            var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
            startEnd.Add(date.Date.AddDays(-1 * (int) cal.GetDayOfWeek(date)));
            startEnd.Add(date.Date.AddDays(-1 * (int) cal.GetDayOfWeek(date) + 6));
            return startEnd;
        }

        public static string TimeSpanToText(TimeSpan hours)
        {
            return $"{Math.Floor(hours.TotalHours):0}:{hours.Minutes:00}";
        }
        
        public static string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Converts decimal to value rounded to quarter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double DecToQuarter(double value)
        {
            double wholeNum = Math.Truncate(value);
            double decPart = value - wholeNum;

            if (decPart >= .875) decPart = 1;
            else if (decPart >= .625) decPart = .75;
            else if (decPart >= .375) decPart = .5;
            else if (decPart >= .125) decPart = .25;
            else decPart = 0;

            return wholeNum + decPart;
        }


        public abstract void Dispose();

        public abstract void VerifySql();
    }
}