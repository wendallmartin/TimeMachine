﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace TheTimeApp.TimeData
{
    [Serializable]
    public class TimeData
    {
        public delegate void ConnectionChangedDel(bool connected);
    
        public delegate void TimeDataUpdatedDel(List<Day> data);

        public List<SqlCommand> Commands = new List<SqlCommand>();

        private List<Day> days;
        
        /// <summary>
        /// This is the UI user name.
        /// </summary>
        [OptionalField]
        public string CurrentUserName;

        [OptionalField]
        private List<User> _users = new List<User>();
        
        [OptionalField]
        private Time _inprogress;

        [NonSerialized]
        private static readonly byte[] Iv = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xCD, 0xEF };
        
        [NonSerialized]
        private static readonly byte[] BKey = { 27, 35, 75, 232, 73, 52, 87, 99 };
        
        [NonSerialized]
        public ConnectionChangedDel ConnectionChangedEvent;
        
        [NonSerialized]
        public ConnectionChangedDel UpdateChangedEvent;

        [NonSerialized] 
        public TimeDataUpdatedDel TimeDataUpdated;
        
        [NonSerialized]
        private static object readWrite = new object();
        
        [NonSerialized] 
        public SqlServerHelper SqlHelper;

        [NonSerialized] 
        public static TimeData TimeDataBase;
        
        public TimeData()
        {
            days = new List<Day>();
            _inprogress = new Time();
        }

        public void SetUpSqlServer()
        {
            if(SqlHelper != null)
                return;
            
            SqlHelper = new SqlServerHelper(Commands);
            
            if(string.IsNullOrEmpty(CurrentUserName))
                AddUser();
            
            SqlHelper.ConnectionChangedEvent += OnConnectionChanged;
            SqlHelper.UpdateChangedEvent += OnUpdateChanged;
            SqlHelper.TimeDateaUpdate += OnTimeDataUpdate;
        }

        [OnDeserialized]
        private void OnDesirialize(StreamingContext context)
        {
            if(_users == null)
                _users = new List<User>();

            if (_users.Count == 0)
            {
                CurrentUserName = "";
                AddUser();
            }
            if (string.IsNullOrEmpty(CurrentUserName))
            {
                CurrentUserName = _users[0].UserName;
            }
        }

        public void AddUser()
        {
            if(AppSettings.SQLEnabled == "true" && SqlHelper == null)
                SetUpSqlServer();
            
            while (_users.Count == 0)
            {
                EnterUser newUserWin = new EnterUser();
                newUserWin.ShowDialog();
                if (newUserWin.UserText != "new user")// todo must perform more professional check for user input
                {
                    AddUser(newUserWin.UserText);    
                }
            }
        }
        
        public void AddUser(string text)
        {
            if (_users.All(u => u.UserName != text))
            {
                _users.Add(new User(text, "", days)); // pump day into user 
                days = new List<Day>();
                CurrentUserName = text;
                SqlHelper?.CreateUser(CurrentUserName);
            }
            else
            {
                MessageBox.Show(@"User already exists!!!");
            }
        }

        public List<User> Users => _users;

        public List<Day> Days
        {
            get{
                if (TimeDataBase._users.Any(u => u.UserName == TimeDataBase.CurrentUserName))
                {
                    return TimeDataBase._users.First(u => u.UserName == TimeDataBase.CurrentUserName).Days ;    
                }
                return new List<Day>();
            }
            set{
                try
                {
                    _users.First(u => u.UserName == CurrentUserName).Days = value;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    throw;
                }
            }
        }

        public void Revert()
        {

        }

        public void Redo()
        {

        }
        
        private void OnTimeDataUpdate(List<Day> list)
        {
            Debug.WriteLine("Time data base update");
            TimeDataUpdated?.Invoke(list);
        }

        private void OnUpdateChanged(bool uptodate)
        {
            UpdateChangedEvent?.Invoke(uptodate);
        }
        
        private void OnConnectionChanged(bool connected)
        {
            ConnectionChangedEvent?.Invoke(connected);
        }

        // orginizes the days by date
        public void SortDays()
        {
            if (Days == null || Days.Count == 0)
                return;

            foreach (Day day in days)
            {
                day.Times.Sort();// sort times within days
            }
            
            days.Sort((a, b) => a.Date.CompareTo(b.Date));
        }
        
        /// <summary>
        /// serializes the class to file
        /// arg given as string
        /// </summary>
        /// <param name="toFile"> Optional file path arg.</param>
        public void Save(string toFile = "")
        {
            lock (readWrite)
            {
                string file = toFile;

                if (file == "")
                    file = AppSettings.DataPath;

                SortDays();
            
                if (!File.Exists(file))
                    File.Create(file).Close();

                try
                {
                    using (var fs = new FileStream(file, FileMode.Create))
                    {
                        using (var des = new DESCryptoServiceProvider())
                        {
                            using (Stream cryptoStream = new CryptoStream(fs, des.CreateEncryptor(BKey, Iv), CryptoStreamMode.Write))
                            {
                                var binaryFormatter = new BinaryFormatter();
                                binaryFormatter.Serialize(cryptoStream, this);
                                cryptoStream.Flush();
                            }
                        }
                    }
                }
                catch (Exception eSerialize)
                {
                    MessageBox.Show(eSerialize.ToString());
                }    
            }
        }

        /// <summary>
        /// This calls TimeData constructer which sets
        /// up the sql server.
        /// </summary>
        /// <returns></returns>
        public static void Load()
        {
            lock (readWrite)
            {
                string file = AppSettings.DataPath;
                if (!File.Exists(file))
                {
                    MessageBox.Show(@"File not found! Starting over with new data in default location.");
                    AppSettings.DataPath = "time.tdf";
                    // we assume this is the first instance of the app so the data file must be created
                    TimeDataBase = new TimeData();
                    return;
                }

                TimeData data = new TimeData();
                //if files is not found, create file
                // Profile exists and can be loaded.
                try
                {
                    using (var fs = new FileStream(file, FileMode.Open))
                    {
                        using (var des = new DESCryptoServiceProvider())
                        {
                            using (Stream cryptoStream = new CryptoStream(fs, des.CreateDecryptor(BKey, Iv), CryptoStreamMode.Read))
                            {
                                var binaryFormatter = new BinaryFormatter();
                                data = (TimeData) binaryFormatter.Deserialize(cryptoStream);
                            }
                        }
                    }
                }
                catch (Exception eDeserialize)
                {
                    MessageBox.Show(eDeserialize.ToString());
                    TimeDataBase = new TimeData();
                }

                TimeDataBase = data;
            }
        }

        private bool DatesAreInTheSameWeek(DateTime date1, DateTime date2)
        {
            if (System.Globalization.DateTimeFormatInfo.CurrentInfo != null)
            {
                var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
                var d1 = date1.Date.AddDays(-1 * (int) cal.GetDayOfWeek(date1));
                var d2 = date2.Date.AddDays(-1 * (int) cal.GetDayOfWeek(date2));

                return d1 == d2;
            }

            return false;
        }
        
        public double HoursInWeek(DateTime weekdDateTime)
        {
            double total = 0;
            foreach (Day day in Days)
            {
                if (DatesAreInTheSameWeek(weekdDateTime, day.Date))
                {
                    total += day.HoursAsDec();
                }
            }

            return total;
        }

        /// <summary>
        /// Removes specified time from data base
        /// </summary>
        /// <param name="time"></param>
        public void DeleteTime(Time time)
        {
            foreach (Day day in Days)
            {
                for (int i = 0; i < day.Times.Count; i++)
                {
                    if (day.Times[i] == time)
                    {
                        day.Times.Remove(day.Times[i]);
                    }
                }
            }
            
            Save();
            
            SqlHelper?.RemoveTime(time);
        }

        /// <summary>
        /// removes the specified day from date base
        /// </summary>
        /// <param name="date"></param>
        public void DeleteDay(DateTime date)
        {
            for (int i = 0; i < Days.Count; i++)
            {
                if (Days[i].Date == date)
                {
                    Days.RemoveAt(i);
                    break;
                }
            }
            
            Save();
            
            SqlHelper?.RemoveDay(date);
            
        }
        
        /// <summary>
        /// removes specified week from date based
        /// </summary>
        /// <param name="date"></param>
        public void DeleteWeek(DateTime date)
        {
            for (int i = 0; i < Days.Count; i++)
            {
                if (DatesAreInTheSameWeek(date, Days[i].Date))
                {
                    Days.RemoveAt(i);
                }
            }
            Save();
            
            SqlHelper?.RemoveWeek(date);
            
        }

        public Day CurrentDay()
        {
            Day currentDay = new Day(new DateTime(2000,1,1));
            bool containsDay = false;
            foreach (Day day in Days)
            {
                if (day.Date.Year == DateTime.Now.Year && day.Date.Month == DateTime.Now.Month && day.Date.Day == DateTime.Now.Day)
                {
                    containsDay = true;
                    break;
                }
            }

            if (!containsDay)
            {
                SqlHelper?.InsertDay(new Day(DateTime.Now));
                Days.Add(new Day(DateTime.Now));
            }

            foreach (Day day in Days)
            {
                if (day.Date.Year == DateTime.Now.Year && day.Date.Month == DateTime.Now.Month && day.Date.Day == DateTime.Now.Day)
                {
                    currentDay =  day;
                }
            }

            return currentDay;
        }

        public void PunchIn()
        {
            _inprogress = new Time();
            _inprogress.PunchIn();
            
            CurrentDay().AddTime(_inprogress);
            
            Save();
            
            SqlHelper?.InsertTime(_inprogress);
        }

        public void PunchOut()
        {
            if (_inprogress == null)
            {
                _inprogress = Days.Last().Times.Last();
            }

            Time prev = new Time(){TimeIn = _inprogress.TimeIn, TimeOut = _inprogress.TimeOut};
            _inprogress.PunchOut();
            
            Save();
            
            SqlHelper?.SqlUpdateTime(prev, _inprogress);
        }

        public string ConverWeekToText(DateTime date)
        {
            string result = "";
            result += "Week " + date.Date.Month + "\\" + date.Date.Day + "\\" + date.Date.Year;
            List<Day> daysinweek = Days.Where(d => DatesAreInTheSameWeek(d.Date, date)).ToList(); 
            foreach (Day day in daysinweek)
            {
                result += "\n   " + day.Date.Month + "\\" + day.Date.Day + "\\" + day.Date.Year + " Hours = " + day.Hours().ToString(@"hh\:mm");
                result += "\n " + day.Details;
                result += "\n--------------------------------------------------------";
            }
            result += "\n -------------------------------";
            result += "\n Total hours = " + HoursInWeek(date);
            return result;
        }

        public void UpdateDetails(Day day, string details)
        {
            day.Details = details;
            
            Save();
            
            SqlHelper?.UpdateDetails(day);
        }

        public void UpdateTime(Time prev, Time upd)
        {
            for (int dayIndex = 0; dayIndex < Days.Count; dayIndex++)
            {
                Day day = Days[dayIndex];
                for (int timeIndex = 0; timeIndex < day.Times.Count; timeIndex++)
                {
                    Time time = day.Times[timeIndex];
                    if (time.TimeIn == prev.TimeIn && time.TimeOut == prev.TimeOut)
                    {
                        Days[dayIndex].Times[timeIndex] = upd;
                    }
                }
            }
            
            Save();
            
            SqlHelper?.SqlUpdateTime(prev, upd);
            
        }

        /// <summary>
        /// Initualizes users from SQL server by erasing all
        /// users and reloading.
        /// </summary>
        public void LoadCurrentUserFromSql()
        {
            var sqLusers = SqlHelper?.GetAllTables();
            _users.Clear();
            if (sqLusers != null)
            {
                foreach (string name in sqLusers)
                {
                    _users.Add(new User(name, "", SqlHelper?.Load(name + "_TimeTable").OrderBy(d => d.Date).ToList()));
                }
            }
        }

        /// <summary>
        /// returns true if we did not punch out.
        /// </summary>
        /// <returns></returns>
        public bool ClockedIn()
        {
            Time time = Days.LastOrDefault()?.Times.LastOrDefault();
            return time != null && time.IsPunchedIn();
        }

        public void DeleteUser(string username)
        {
            if(TimeDataBase._users.Any(u => u.UserName == username))
            {
                TimeDataBase._users.Remove(_users.Find(u => u.UserName == username));

                if (AppSettings.SQLEnabled == "true")
                {
                    var result = MessageBox.Show(@"Do you want to remove user from SQL as well?", @"Warning", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        SqlHelper?.RemoveUser(username);
                    }    
                }
            }
            else
            {
                MessageBox.Show(@"User does not exist!");
            }
        }
    }
}