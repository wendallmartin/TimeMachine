using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Timers;

namespace TheTimeApp.TimeData
{
    public abstract class TimeServer
    {
        private static readonly object IsConnectedLock = new object();
        private static readonly object SqlServerLock = new object();
        private static readonly object SqlPullLock = new object();
        private static readonly object SqlPushLock = new object();
        
        public abstract string SqlCurrentUser { get; set; }
        protected bool _wasConnected;
        protected readonly Timer _connectionRetry = new Timer(1000);
        protected SqlMode SqlMode { get; set; }
        protected List<SerilizeSqlCommand> Commands;


        public int Port { get; set; }
        
        protected DataTable _dataTable = new DataTable();

        public bool SqlEnabled { get; set; } = true;

        #region props

        public abstract List<string> UserNames { get; }
        

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
        
        public abstract int DeleteUser(string username);

        public abstract void AddDay(Day day);
        
        public abstract int DeleteDay(DateTime date);
        
        public abstract int DeleteRange(DateTime start, DateTime end);
        
        public abstract void PunchIn();

        public abstract void PunchOut();

        public abstract Day CurrentDay();
        
        public abstract int DeleteTime(double key);
        
        public abstract int UpdateDetails(DateTime date, string details);
        
        public abstract int UpdateTime(double key, Time upd);
        
        public abstract string GetRangeAsText(DateTime dateA, DateTime dateB);

        public string UserTable => "Users_TimeTable";

        /// <summary>
        /// Returns user name converted to table name.
        /// </summary>
        /// <returns></returns>
        public string ToTimeTableName(string user)
        {
            return "Time_" + user.Replace(' ', '_') + "_TimeTable";
        }


        /// <summary>
        /// Returns user name converted to table name.
        /// </summary>
        /// <returns></returns>
        public string ToDayTableName(string user)
        {
            return "Day_" + user.Replace(' ', '_') + "_TimeTable";
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
    }
}