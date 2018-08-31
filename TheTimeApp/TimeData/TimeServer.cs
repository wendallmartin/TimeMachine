using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Timers;
using NLog;

namespace TheTimeApp.TimeData
{
    public abstract class TimeServer
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly object IsConnectedLock = new object();
        private static readonly object SqlServerLock = new object();
        private static readonly object SqlPullLock = new object();
        private static readonly object SqlPushLock = new object();
        
        public static string SqlCurrentUser { get; set; }
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
        protected SqlConnection _connection = new SqlConnection();// Only referenced from SqlConnection property!

        #endregion
        
        
        
        
        
        public abstract bool IsClockedIn();

        public abstract void AddUser(User user);
        
        public abstract List<string> UserNames();
        
        public abstract int DeleteUser(string username);

        public abstract Day CurrentDay();
        
        public abstract void AddDay(Day day);

        public abstract List<Day> DaysInRange(DateTime a, DateTime b);
        
        public abstract int DeleteDay(DateTime date);
        
        public abstract List<Day> AllDays();

        public abstract double HoursInRange(DateTime a, DateTime b);
        
        public abstract int DeleteRange(DateTime start, DateTime end);
        
        public abstract void PunchIn();

        public abstract void PunchOut();
        
        public abstract List<Time> AllTimes();

        public abstract int DeleteTime(double key);
        
        public abstract int UpdateDetails(DateTime date, string details);
        
        public abstract int UpdateTime(double key, Time upd);

        public abstract double MaxTimeId(string tablename = "");
        
        public abstract List<Time> TimesinRange(DateTime dateA, DateTime dateB);
        
        public abstract string GetRangeAsText(DateTime dateA, DateTime dateB);

        public string UserTable => "Users_TimeTable";

        public abstract DateTime MinDate();
        
        public abstract DateTime MaxDate();

        public abstract void RePushToServer();
        
        public abstract void LoadFromServer();

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

        public static string DateTimeSqLite(DateTime datetime)
        {
            string dateTimeFormat = "{0}-{1}-{2} {3}:{4}:{5}.{6}";
            return string.Format(dateTimeFormat, datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second,datetime.Millisecond);
        }
        
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

        public abstract void Dispose();
    }
}