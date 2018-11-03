using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using MySql.Data.MySqlClient;
using NLog;
using NLog.Fluent;

namespace TheTimeApp.TimeData
{
    public class DataBaseManager : TimeServer
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        public readonly TimeServer _primary;

        private readonly TimeServer _secondary;

        public static DataBaseManager Instance;

        private DataBaseManager()
        {
            _primary = Sqlite.LoadFromFile();
            
            _primary.ProgressChangedEvent += OnProgressChangedEvent;
            _primary.ProgressFinishEvent += OnProgressFinishEvent;
            
            if (AppSettings.Instance.SqlEnabled == "true")
            {
                if (AppSettings.Instance.SqlType == "MySql" && !string.IsNullOrEmpty(AppSettings.Instance.MySqlServer))
                {
                    MySqlConnectionStringBuilder mysqlBuiler = new MySqlConnectionStringBuilder()// Database is set later in constructor!
                    {
                        Server = AppSettings.Instance.MySqlServer,
                        UserID = AppSettings.Instance.MySqlUserId,
                        Password = AppSettings.Instance.MySqlPassword,
                        Port = (uint) AppSettings.Instance.MySqlPort,
                        SslMode = AppSettings.Instance.MySqlSsl == "true" ? MySqlSslMode.Required : MySqlSslMode.None 
                    };
                    try
                    {
                        _secondary = new MySql(mysqlBuiler, MySql.UpdateModes.Async);
                        
                        // Associate events
                        _secondary.TimeDateaUpdate += OnTimeDateaUpdate;
                        _secondary.ProgressChangedEvent += OnProgressChangedEvent;
                        _secondary.ProgressFinishEvent += OnProgressFinishEvent;
                        _secondary.ConnectionChangedEvent += OnConnectionChangedEvent;
                        _secondary.UpdateChangedEvent += OnUpdateChangedEvent;

                        var daysInWeek = _primary.DaysInRange(StartEndWeek(DateTime.Today)[0], StartEndWeek(DateTime.Today)[1]);
                        
                        if(_secondary.ServerState == State.Connected)
                            _secondary.Push(daysInWeek);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        MessageBox.Show(e.Message);
                    }
                        
                }
                else if (AppSettings.Instance.SqlType == "Azure" && !string.IsNullOrEmpty(AppSettings.Instance.AzureDateSource))
                {
                    MessageBox.Show("Azure sql not implemented!");
                }
            }
        }

        private void OnTimeDateaUpdate(List<Day> data) => TimeDateaUpdate?.Invoke(data); 

        private void OnProgressChangedEvent(float value) => ProgressChangedEvent?.Invoke(value); 

        private void OnProgressFinishEvent() => ProgressFinishEvent?.Invoke();

        private void OnConnectionChangedEvent(bool value) => ConnectionChangedEvent?.Invoke(value);

        private void OnUpdateChangedEvent(List<SerilizeSqlCommand> value) => UpdateChangedEvent?.Invoke(value);

        public static void Initualize()
        {
            logger.Info("Initualize");
            Instance = new DataBaseManager();
        }

        /// <summary>
        /// If secondary buffer is mysql,
        /// save buffer to disk.
        /// </summary>
        public void SaveBuffer()
        {
            logger.Info("SaveBuffer");
            if(_secondary != null && _secondary is MySql mySql) mySql.SaveBuffer();
        }
        
        public override bool IsClockedIn()
        {
            return _primary.IsClockedIn();
        }

        public override void AddUser(User user)
        {
            logger.Info("AddUser......");
            _primary.AddUser(user);
            _secondary?.AddUser(user);
            logger.Info("AddUser......FINISHED!!!");
        }
        
        public override List<string> UserNames()
        {
            return _primary.UserNames();
        }

        public override void DeleteUser(string username)
        {
            logger.Info("DeleteUser......");
            _primary.DeleteUser(username);
            _secondary?.DeleteUser(username);
            logger.Info("DeleteUser......FINISHED!!!");
        }

        public override void AddDay(Day day)
        {
            logger.Info("AddDay......");
            _primary.AddDay(day);
            _secondary?.AddDay(day);
            logger.Info("AddDay......FINISHED!!!");
        }

        public override List<Day> DaysInRange(DateTime a, DateTime b)
        {
            return _primary.DaysInRange(a, b);
        }

        public override void DeleteDay(DateTime date)
        {
            logger.Info("DeleteDay......");
            _primary.DeleteDay(date);
            _secondary?.DeleteDay(date);
            logger.Info("DeleteDay......FINISHED!!!");
        }

        public override List<Day> AllDays()
        {
            return _primary.AllDays();
        }

        public override TimeSpan HoursInRange(DateTime a, DateTime b)
        {
            return _primary.HoursInRange(a, b);
        }

        public override void DeleteRange(DateTime start, DateTime end)
        {
            logger.Info("DeleteRange......");
            _primary.DeleteRange(start, end);
            _secondary?.DeleteRange(start,end);
            logger.Info("DeleteRange......FINISHED!!!");
        }

        public override void PunchIn(string key)
        {
            logger.Info("PunchIn......");
            _primary.PunchIn(key);
            _secondary?.PunchIn(key);
            logger.Info("PunchIn......FINISHED!!!");
        }

        public override void PunchOut(string key)
        {
            logger.Info("PunchOut......");
            _primary.PunchOut(key);
            _secondary?.PunchOut(key);
            logger.Info("PunchOut......FINISHED!!!");
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

        public override void DeleteTime(string key)
        {
            logger.Info("DeleteTime......");
            _primary.DeleteTime(key);
            _secondary?.DeleteTime(key);
            logger.Info("DeleteTime......FINISHED!!!");
        }

        public override void UpdateDetails(DateTime date, string details)
        {
            logger.Info("UpdateDetails.......");
            _primary.UpdateDetails(date, details);
            _secondary?.UpdateDetails(date, details);
            logger.Info("UpdateDetails.......FINISHED!!!");
        }

        public override void UpdateTime(string key, Time upd)
        {
            logger.Info("UpdateTime......");
            _primary.UpdateTime(key, upd);
            _secondary?.UpdateTime(key, upd);
            logger.Info("UpdateTime......FINISHED!!!");
        }

        public override string LastTimeId()
        {
            return _primary.LastTimeId();
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

        /// <summary>
        /// Pushes given day list
        /// to secondary server.
        /// </summary>
        /// <param name="days"></param>
        public override void Push(List<Day> days)
        {
            logger.Info("Push......");
            _secondary?.Push(days);
            logger.Info("Push......FINISHED!!!");
        }

        /// <summary>
        /// Loads list of days
        /// from secondary server.
        /// </summary>
        /// <returns></returns>
        public override List<Day> Pull()
        {
            logger.Info("Pull");
            return _secondary == null ? new List<Day>() : _secondary.Pull();
        }

        /// <summary>
        /// Pulls secondary to primary,
        /// in context of current user.
        /// </summary>
        public void PullSecondaryToPrimary()
        {
            logger.Info("PullSecondaryToPrimary......");
            try{ _primary.Push(Pull()); }
            catch{/* eat them! */}
            logger.Info("PullSecondaryToPrimary......FINISHED!!!");
        }

        /// <summary>
        /// Pushes primary server to secondary,
        /// in context of current user.
        /// </summary>
        public void PushPrimaryToSecondary()
        {
            logger.Info("PushPrimaryToSecondary......");
            try { _secondary?.Push(_primary.Pull()); }
            catch { /* eat em! */}
            logger.Info("PushPrimaryToSecondary......FINISHED!!!");
        }

        public override void Dispose()
        {
            logger.Info("Disposing......");
            _primary?.Dispose();
            _secondary?.Dispose();
            logger.Info("Disposing......Finished!!!");
        }

        /// <summary>
        /// Verifies primary
        /// and secondary server.
        /// </summary>
        public override void VerifySql()
        {
            try
            {
                logger.Info("VerifySql.....");
                _primary?.VerifySql();
                _secondary?.VerifySql();
                logger.Info("VerifySql.....FINISHED!!!");
            }
            catch{ /* eat em! */}
        }
    }
}