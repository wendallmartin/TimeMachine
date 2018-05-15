using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Timer = System.Timers.Timer;

namespace TheTimeApp.TimeData
{
    public delegate void ConnectionChangedDel(bool connected);
    
    [Serializable]
    public class TimeData
    {
        private int _stateIndex = 0;
        private static object readWrite = new object();

        [NonSerialized]
        public ConnectionChangedDel ConnectionChangedEvent;
        
        
        [NonSerialized]
        public ConnectionChangedDel UpdateChangedEvent;
        
        [NonSerialized]
        public static List<SqlCommand> Commands = new List<SqlCommand>();

        private List<Day> days;

        [NonSerialized]
        private static readonly byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xCD, 0xEF };
        
        [NonSerialized]
        private static readonly byte[] bKey = { 27, 35, 75, 232, 73, 52, 87, 99 };

        private Time _inprogress;

        [NonSerialized]
        private SQLServerHelper _sqlHelper;

        public TimeData()
        {
            _sqlHelper = new SQLServerHelper(Commands);
            _sqlHelper.ConnectionChangedEvent += OnConnectionChanged;
            _sqlHelper.UpdateChangedEvent += OnUpdateChanged;
            days = new List<Day>();
            _inprogress = new Time();
        }

        [OnDeserialized]
        private void InitSqlHelper(StreamingContext context)
        {
            if (_sqlHelper == null)
            {
                _sqlHelper = new SQLServerHelper(Commands);
                _sqlHelper.ConnectionChangedEvent += OnConnectionChanged;
                _sqlHelper.UpdateChangedEvent += OnUpdateChanged;
            }
        }

        public List<Day> Days
        {
            get{ return days; }
        }

        public void Revert()
        {

        }

        public void Redo()
        {

        }

        private void OnUpdateChanged(bool uptodate)
        {
            UpdateChangedEvent?.Invoke(uptodate);
        }
        
        private void OnConnectionChanged(bool connected)
        {
            ConnectionChangedEvent?.Invoke(connected);
        }

        /// <summary>
        /// serializes the class to file
        /// arg given as string
        /// </summary>
        /// <param name="file"></param>
        public void Save()
        {
            lock (readWrite)
            {
                string file = AppSettings.DataPath;

                SortDays();
            
                if (!File.Exists(file))
                    File.Create(file).Close();

                try
                {
                    using (var fs = new FileStream(file, FileMode.Create))
                    {
                        using (var des = new DESCryptoServiceProvider())
                        {
                            using (Stream cryptoStream = new CryptoStream(fs, des.CreateEncryptor(bKey, IV), CryptoStreamMode.Write))
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

        // orginizes the days by date
        public void SortDays()
        {
            days.Sort((a, b) => a.Date.CompareTo(b.Date));

            for(int i = 0; i < days.Count; i++)
            {
                if (!days[i].HasTime() && days[i].Details == "")
                {
                    days.RemoveAt(i);
                }
            }
        }

        public static TimeData Load()
        {
            lock (readWrite)
            {
                string file = AppSettings.DataPath;

                TimeData data = null;
                if (File.Exists(file))
                {
                    //if files is not found, create file
                    // Profile exists and can be loaded.
                    try
                    {
                        using (var fs = new FileStream(file, FileMode.Open))
                        {
                            using (var des = new DESCryptoServiceProvider())
                            {
                                using (Stream cryptoStream = new CryptoStream(fs, des.CreateDecryptor(bKey, IV), CryptoStreamMode.Read))
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
                        return new TimeData();
                    }
                }
                else
                {
                    // we assume this is the first instance of the app so the data file must be created
                    return new TimeData();
                }
                return data;   
            }
        }

        private bool DatesAreInTheSameWeek(DateTime date1, DateTime date2)
        {
            var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
            var d1 = date1.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date1));
            var d2 = date2.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date2));

            return d1 == d2;
        }
        
        public double HoursInWeek(DateTime weekdDateTime)
        {
            double total = 0;
            foreach (Day day in days)
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

            _sqlHelper.DeleteTime(time);
            Save();
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

            _sqlHelper.RemoveDay(date);
            Save();
        }
        
        /// <summary>
        /// removes specified week from date based
        /// </summary>
        /// <param name="date"></param>
        public void DeleteWeek(DateTime date)
        {
            for (int i = 0; i < days.Count; i++)
            {
                if (DatesAreInTheSameWeek(date, days[i].Date))
                {
                    days.RemoveAt(i);
                }
            }

            _sqlHelper.RemoveWeek(date);
            Save();
        }

        /// <summary>
        /// returns true if data base contains current day
        /// </summary>
        /// <returns></returns>
        public bool ContainsCurrentDay()
        {
            foreach (Day day in days)
            {
                if (day.Date.Year == DateTime.Now.Year && day.Date.Month == DateTime.Now.Month && day.Date.Day == DateTime.Now.Day)
                {
                    return true;
                }
            }
            return false;
        }

        public Day CurrentDay()
        {
            Day currentDay = new Day(new DateTime(2000,1,1));
            bool containsDay = false;
            foreach (Day day in days)
            {
                if (day.Date.Year == DateTime.Now.Year && day.Date.Month == DateTime.Now.Month && day.Date.Day == DateTime.Now.Day)
                {
                    containsDay = true;
                    break;
                }
            }

            if (!containsDay)
                days.Add(new Day(DateTime.Now));

            foreach (Day day in days)
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
        }

        public void PunchOut()
        {
            _inprogress.PunchOut();
            CurrentDay().AddTime(_inprogress);
            _sqlHelper.InsertTime(_inprogress);
            Save();
        }

        public string ConverWeekToText(DateTime date)
        {
            string result = "";
            result += "Week " + date.Date.Month + "\\" + date.Date.Day + "\\" + date.Date.Year; 

            foreach (Day day in days)
            {
                if(DatesAreInTheSameWeek(date, day.Date))
                {
                    day.Emailed = true;
                    result += "\n   " + day.Date.Month + "\\" + day.Date.Day + "\\" + day.Date.Year + " Hours = " + day.Hours().ToString(@"hh\:mm");
                    result += "\n " + day.Details;
                    result += "\n--------------------------------------------------------";
                }
            }
            result += "\n -------------------------------";
            result += "\n Total hours = " + HoursInWeek(date);
            return result;
        }

        public void UpdateDetails(Day day, string details)
        {
            day.Details = details;
            _sqlHelper.UpdateDetails(day);
        }
    }
}