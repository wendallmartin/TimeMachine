using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;
using NLog;

namespace TheTimeApp.TimeData
{
    public class DataBaseManager : TimeServer
    {
        private NLog.Logger logger = LogManager.GetCurrentClassLogger();
        
        private TimeServer _primary;

        private readonly List<TimeServer> _secondary = new List<TimeServer>();

        public static DataBaseManager Instance;

        private DataBaseManager()
        {
            _primary = Sqlite.LoadFromFile();
            
            if (AppSettings.SqlEnabled == "true")
            {
                if (AppSettings.SqlType == "MySql" && !string.IsNullOrEmpty(AppSettings.MySqlServer))
                {
                    MySqlConnectionStringBuilder mysqlBuiler = new MySqlConnectionStringBuilder()
                    {
                        Server = AppSettings.MySqlServer,
                        UserID = AppSettings.MySqlUserId,
                        Password = AppSettings.MySqlPassword,
                        Database = AppSettings.MySqlDatabase,
                        Port = (uint) AppSettings.MySqlPort,
                        SslMode = MySqlSslMode.None// todo add mysql ssl setting
                    };
                    try
                    {
                        _secondary.Add(new MySql(mysqlBuiler.ConnectionString));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        MessageBox.Show(e.Message);
                    }
                        
                }
                else if (AppSettings.SqlType == "Azure" && !string.IsNullOrEmpty(AppSettings.AzureDateSource))
                {
                    MessageBox.Show("Azure sql not implemented!");
                }
            }
        }

        public static void Initulize()
        {
            Instance = new DataBaseManager();
        }
        
        public override bool IsClockedIn()
        {
            return _primary.IsClockedIn();
        }

        public override void AddUser(User user)
        {
            _primary.AddUser(user);
            _secondary.ForEach(s => s.AddUser(user));
        }
        
        public override List<string> UserNames()
        {
            return _primary.UserNames();
        }

        public override int DeleteUser(string username)
        {
            var result = _primary.DeleteUser(username);
            _secondary.ForEach(s => s.DeleteUser(username));
            return result;
        }

        public override void AddDay(Day day)
        {
            _primary.AddDay(day);
            _secondary.ForEach(s => s.AddDay(day));
        }

        public override List<Day> DaysInRange(DateTime a, DateTime b)
        {
            return _primary.DaysInRange(a, b);
        }

        public override int DeleteDay(DateTime date)
        {
            var result = _primary.DeleteDay(date);
            _secondary.ForEach(s => s.DeleteDay(date));
            return result;
        }

        public override List<Day> AllDays()
        {
            return _primary.AllDays();
        }

        public override TimeSpan HoursInRange(DateTime a, DateTime b)
        {
            return _primary.HoursInRange(a, b);
        }

        public override int DeleteRange(DateTime start, DateTime end)
        {
            var result = _primary.DeleteRange(start, end);
            _secondary.ForEach(s => s.DeleteRange(start,end));
            return result;
        }

        public override void PunchIn()
        {
            _primary.PunchIn();
            _secondary.ForEach(s => s.PunchIn());
        }

        public override void PunchOut()
        {
            _primary.PunchOut();
            _secondary.ForEach(s => s.PunchOut());
        }

        public override List<Time> AllTimes()
        {
            return _primary.AllTimes();
        }

        public override Day CurrentDay()
        {
            Day day = _primary.CurrentDay();
            return day;
        }

        public override int DeleteTime(double key)
        {
            var result = _primary.DeleteTime(key);
            _secondary.ForEach(s => s.DeleteTime(key));
            return result;
        }

        public override int UpdateDetails(DateTime date, string details)
        {
            var result = _primary.UpdateDetails(date, details);
            _secondary.ForEach(s => s.UpdateDetails(date, details));
            return result;
        }

        public override int UpdateTime(double key, Time upd)
        {
            var result = _primary.UpdateTime(key, upd);
            _secondary.ForEach(s => s.UpdateTime(key, upd));
            return result;
        }

        public override double MaxTimeId(string tablename = "")
        {
            return _primary.MaxTimeId(tablename);
        }

        public override List<Time> TimesinRange(DateTime dateA, DateTime dateB)
        {
            return _primary.TimesinRange(dateA, dateB);
        }

        public override string GetRangeAsText(DateTime dateA, DateTime dateB)
        {
            return _primary.GetRangeAsText(dateA,dateB);
        }

        public override DateTime MinDate()
        {
            return _primary.MinDate();
        }

        public override DateTime MaxDate()
        {
            return _primary.MaxDate();
        }

        public override void RePushToServer()
        {
            _primary.RePushToServer();
        }

        public override void LoadFromServer()
        {
            _primary.LoadFromServer();
        }

        public override void Dispose()
        {
            logger.Info("Disposing......");
            _primary.Dispose();
            _secondary.ForEach(s => s.Dispose());
            logger.Info("Disposing......Finished!!!");
        }
    }
}