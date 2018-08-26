using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;
using NLog;

namespace TheTimeApp.TimeData
{
    /// <summary>
    /// The local database
    /// </summary>
    public class Sqlite : TimeServer
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private SQLiteConnection _connection;
        private string _connectionString;

        public Sqlite(string conStringBuilder)
        {
            logger.Info("Initalize......");
            _connection = new SQLiteConnection(conStringBuilder);
            _connectionString = conStringBuilder;
            
            if (!File.Exists("TimeData.sqlite"))
            {
                SQLiteConnection.CreateFile("TimeData.sqlite");
            }
            
            _connection.Open();
            
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                if (AppSettings.MySqlDatabase == "")
                    AppSettings.MySqlDatabase = "TimeDataBase";
                
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{UserTable}]([Name] TEXT , [Rate] TEXT , [Unit] TEXT , [Active] TEXT )";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{DayTableName}] ( [Date] TEXT, [Details] TEXT )";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{TimeTableName}] ([Date] TEXT, [TimeIn] TEXT, [TimeOut] TEXT, [Key] INT )";
                cmd.ExecuteNonQuery();
            }

            
            logger.Info("Initalize......FINISHED!!!");
        }

        public override void Dispose()
        {
            logger.Info("Disposing......");
            _connection.Close();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            logger.Info("Disposing......FINISHED!!!");
        }

        public override List<string> UserNames()
        {
            Debug.WriteLine("Usernames");
            List<string> usernames = new List<string>();
            try
            {
                using (SQLiteCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{UserTable}] ( [Name] TEXT, [Rate] TEXT, [Unit] TEXT, [Active] TEXT )";
                    cmd.ExecuteNonQuery();
                }

                DataTable dataTable = new DataTable();
                using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM {UserTable}", _connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    usernames.Add(row.ItemArray[0].ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            usernames.ForEach(u => Debug.Write(", " + u));
            Debug.WriteLine("");
            return usernames;
        }

        public override bool IsClockedIn()
        {
            Debug.WriteLine("IsClockedIn:");
            object timein;
            object timeout;

                using(SQLiteCommand selectMaxIn = new SQLiteCommand($"Select Max(TimeIn) From {TimeTableName}", _connection))
                {
                    timein = selectMaxIn.ExecuteScalar();
                    if (timein.ToString() == "")
                    {
                        Debug.Write(" false");
                        return false;
                    }
                }

                using (SQLiteCommand selectMaxOut = new SQLiteCommand($"Select Max(TimeOut) From {TimeTableName}", _connection))
                {
                    timeout = selectMaxOut.ExecuteScalar();
                    if (timeout.ToString() == "")
                    {
                        Debug.Write(" false");
                        return false;
                    }
                }
            
            Debug.Write(timein.ToString() == timeout.ToString());
            return timein.ToString() == timeout.ToString();
        }

//        public override string TimeServer.SqlCurrentUser
//        {
//            get{
//                Debug.WriteLine("TimeServer.SqlCurrentUser get");
//                using (SQLiteCommand cmd = _connection.CreateCommand())
//                {
//                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{UserTable}] ( [Name] TEXT, [Rate] TEXT, [Unit] TEXT, [Active] TEXT )";
//                    cmd.ExecuteNonQuery();
//
//                    cmd.CommandText = $"SELECT Name from {UserTable} WHERE Active = \'true\'";
//                    string result =  (string)cmd.ExecuteScalar();
//                    
//                    return result;
//                }
//            }
//            set{
//                logger.Info($"Set current user: {value}");
//                Debug.WriteLine("TimeServer.SqlCurrentUser set");
//                if(!UserNames().Contains(value))
//                    throw new Exception("User does not exist!!!");
//                
//                using (SQLiteCommand cmd = _connection.CreateCommand())
//                {
//                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{UserTable}] ( [Name] TEXT, [Rate] TEXT, [Unit] TEXT, [Active] TEXT )";
//                    cmd.ExecuteNonQuery();
//
//                    cmd.CommandText = $"INSERT INTO {UserTable} (Name, Rate, Unit, Active) Select @Name, @Rate, @Unit, @Active WHERE not exists(select * from {UserTable} where Name = '{value}')";
//                    cmd.Parameters.Add(new SQLiteParameter("Name", value));
//                    cmd.Parameters.Add(new SQLiteParameter("Rate", ""));
//                    cmd.Parameters.Add(new SQLiteParameter("Unit", ""));
//                    cmd.Parameters.Add(new SQLiteParameter("Active", "true"));
//                    cmd.ExecuteNonQuery();
//                    
//                    cmd.CommandText = $"UPDATE {UserTable} SET Active = @Active WHERE Name = '{value}'";
//                    cmd.Parameters.Add(new SQLiteParameter("Active", "true"));
//                    cmd.ExecuteNonQuery();
//
//                    cmd.CommandText = $"UPDATE {UserTable} SET Active = @Active WHERE Name != '{value}'";
//                    cmd.Parameters.Add(new SQLiteParameter("Active", "false"));
//                    cmd.ExecuteNonQuery();
//                }
//            }
//        }

        public override void AddUser(User user)
        {
            logger.Info($"Add user: {user.UserName}..........");
            Debug.WriteLine($"AddUser: {user.UserName}");
            string dayTable = ToDayTableName(user.UserName);
            string timeTable = ToTimeTableName(user.UserName);
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{UserTable}] ( [Name] TEXT, [Rate] TEXT, [Unit] TEXT, [Active] TEXT )";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $"Insert into {UserTable} (Name, Rate, Unit, Active) values (@Name, @Rate, @Unit, @Active)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SQLiteParameter("Name", user.UserName));
                cmd.Parameters.Add(new SQLiteParameter("Rate", 0));
                cmd.Parameters.Add(new SQLiteParameter("Unit", ""));
                cmd.Parameters.Add(new SQLiteParameter("Active", "false"));
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $@"DROP TABLE IF EXISTS [{dayTable}]";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $@"CREATE TABLE [{dayTable}] ( [Date] TEXT, [Details] TEXT )";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $@"DROP TABLE IF EXISTS [{timeTable}]";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $@"CREATE TABLE [{timeTable}] ([Date] TEXT, [TimeIn] TEXT, [TimeOut] TEXT, [Key] INT )";
                cmd.ExecuteNonQuery();
                foreach (Day day in user.Days)
                {
                    cmd.CommandText = $"INSERT INTO {dayTable} VALUES(@Date, @Details)";
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(day.Date)));
                    cmd.Parameters.Add(new SQLiteParameter("Details", day.Details));
                    cmd.ExecuteNonQuery();
                    foreach (Time time in day.Times)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = $"INSERT INTO {timeTable} VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                        cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(time.TimeIn.Date)));
                        cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                        cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                        cmd.Parameters.Add(new SQLiteParameter("Key", MaxTimeId(timeTable) + 1));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            logger.Info($"Add user: {user.UserName}..........FINISHED!!!");
        }

        public override Day CurrentDay()
        {
            Day day = new Day(DateTime.MinValue);
            Debug.WriteLine("CurrentDay:");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"select * from {DayTableName} Where Date = '{DateSqLite(DateTime.Now)}'";
                cmd.Parameters.Add(new SQLiteParameter());
                using (SQLiteDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        day = new Day(Convert.ToDateTime(rdr["Date"].ToString()))
                        {
                            Details = rdr["Details"].ToString()
                        };
                    }
                }
            }

            if (day.Date == DateTime.MinValue)
            {
                day = new Day(DateTime.Now);
                AddDay(day);
            }
            
            Debug.Write(day.ToString());
            return day;
        }

        public override int DeleteTime(double key)
        {
            logger.Info($"Delete time: {key}.........");
            Debug.WriteLine($"DeleteTime: {key}");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {TimeTableName} WHERE Key = @Key";
                cmd.Parameters.Add(new SQLiteParameter("Key", key));
                return cmd.ExecuteNonQuery();
            }
            logger.Info($"Delete time: {key}.........FINISHED!!!");
        }

        public override void AddDay(Day day)
        {
            logger.Info($"Add day: {day.Date}.........");
            Debug.WriteLine($"AddDay: {day}");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {DayTableName} VALUES(@Date, @Details)";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(day.Date.Date)));
                cmd.Parameters.Add(new SQLiteParameter("Details", day.Details));
                cmd.ExecuteNonQuery();
                
                foreach (Time time in day.Times)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = $"INSERT INTO {TimeTableName} VALUES(@Date, @TimeIn, @TimeOut)";
                    cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(time.TimeIn.Date)));
                    cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                    cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                    cmd.ExecuteNonQuery();
                }
            }
            logger.Info($"Add day: {day.Date}.........FINISHED!!!");
        }


        public override int DeleteDay(DateTime date)
        {
            logger.Info($"Delete day: {date}........");
            Debug.WriteLine($"DeleteDay: {date}");
            int result = 0;
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {DayTableName} WHERE Date = @Date";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(date)));
                result += cmd.ExecuteNonQuery();
                
                cmd.CommandText = $"DELETE FROM {TimeTableName} WHERE Date = @Date";
                result += cmd.ExecuteNonQuery();
             }

            logger.Info($"Delete day: {date}........FINISHED!!! Result: {result}");
            return result;
        }

        public override int DeleteRange(DateTime start, DateTime end)
        {
            logger.Info($"Delete range, Start: {start} | End: {end}.........");
            Debug.WriteLine($"DeleteRange: {start}-{end}");
            int result = 0;
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                string stringstart = DateSqLite(start);
                string stringend = DateSqLite(end);
                
                cmd.CommandText = $"DELETE FROM {DayTableName} WHERE Date >= @start AND Date <= @end";
                cmd.Parameters.Add(new SQLiteParameter("start", stringstart));
                cmd.Parameters.Add(new SQLiteParameter("end", stringend));
                result += cmd.ExecuteNonQuery();
                
                cmd.Parameters.Clear();
                
                cmd.CommandText = $"DELETE FROM {TimeTableName} WHERE Date >= @start AND Date <= @end";
                cmd.Parameters.Add(new SQLiteParameter("start", stringstart));
                cmd.Parameters.Add(new SQLiteParameter("end", stringend));
                result += cmd.ExecuteNonQuery();
            }

            logger.Info($"Delete range, Start: {start} | End: {end}..........FINISHED!!! Result: {result}");
            return result;
        }

        public override int DeleteUser(string username)
        {
            logger.Info($"Delete user: {username}.........");
            int result = 0;
            Debug.WriteLine($"DeleteUser: {username}");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"DROP TABLE IF EXISTS [{ToDayTableName(username)}]";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $@"DROP TABLE IF EXISTS [{ToTimeTableName(username)}]";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"DELETE FROM {UserTable} WHERE Name = @Name";
                cmd.Parameters.Add(new SQLiteParameter("Name", username));
                result = cmd.ExecuteNonQuery();
            }
            logger.Info($"Delete user: {username}.........FINISHED!!! Result: {result}");
            return result;
        }

        public override int UpdateDetails(DateTime date, string details)
        {
            logger.Info($"Update details, Date: {date} | Details: {details}.........");
            int result = 0;
            Debug.WriteLine($"UdateDetails: {date}:{details}");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"UPDATE {DayTableName} SET Details = @Details WHERE Date = '{DateSqLite(date.Date)}'";
                cmd.Parameters.Add(new SQLiteParameter("Details", details));
                result = cmd.ExecuteNonQuery();
            }

            logger.Info($"Update details, Date: {date} | Details: {details}.........FINISHED!!! Result: {result}");
            return result;
        }

        public override int UpdateTime(double key, Time upd)
        {
            logger.Info($"Update time, Key: {key} | Update: {upd}...........");
            Debug.WriteLine($"UpdateTime: {key}:{upd}");
            int result = 0;
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"select * from {TimeTableName} Where Date = @Date";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(upd.TimeIn.Date)));
                using (SQLiteDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read())
                    {
                        AddDay(new Day(upd.TimeIn.Date));        
                    }
                }
            }

            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"UPDATE {TimeTableName} SET Date = @Date, TimeIn = @TimeIn, TimeOut = @TimeOut WHERE Key = @Key";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(upd.TimeIn.Date)));
                cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(upd.TimeIn)));
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(upd.TimeOut)));
                cmd.Parameters.Add(new SQLiteParameter("Key", key));
                result = cmd.ExecuteNonQuery();
            }
            
            logger.Info($"Update time, Key: {key} | Update: {upd}...........FINISHED!!! Result: {result}");
            return result;
        }

        public override void PunchIn()
        {
            logger.Info("Punchin");
            Debug.WriteLine("PunchIn");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{TimeTableName}] ([Date] TEXT,[TimeIn] TEXT,[TimeOut] TEXT,[Key] INT)";
                cmd.ExecuteNonQuery();
                
                DateTime now = DateTime.Now;
                cmd.CommandText = $"INSERT INTO {TimeTableName} VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(now.Date)));
                cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(now)));
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(now)));
                cmd.Parameters.Add(new SQLiteParameter("Key", MaxTimeId() + 1));
                cmd.ExecuteNonQuery();
            }
        }

        public override void PunchOut()
        {
            logger.Info("PunchOut");
            Debug.WriteLine("PunchOut");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"UPDATE {TimeTableName} SET TimeOut = @TimeOut WHERE Key = '{MaxTimeId()}'";
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(DateTime.Now)));
                cmd.ExecuteNonQuery();
            }
        }

        public override string GetRangeAsText(DateTime dateA, DateTime dateB)
        {
            logger.Info($"GetRangeAsText, DateA: {dateA} | DateB: {dateB}");
            Debug.WriteLine($"GetRangeAsText: {dateA}-{dateB}");
            string result = "";
            result += dateA.Date.Month + "\\" + dateA.Date.Day + "\\" + dateA.Date.Year + " to " + dateB.Date.Month + "\\" + dateB.Date.Day + "\\" + dateB.Date.Year;
            
            foreach (Day day in DaysInRange(dateA,dateB))
            {
                result += "\n   " + day.Date.Month + "\\" + day.Date.Day + "\\" + day.Date.Year + " Hours = " + day.Hours().ToString(@"hh\:mm");
                result += "\n " + day.Details;
                result += "\n--------------------------------------------------------";
            }
            result += "\n -------------------------------";
            result += "\n Total hours = " + HoursInRange(dateA, dateB);
            return result;
        }

        public override double HoursInRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"HoursInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"HoursInRange: {dateA}-{dateB}");
            var times = DaysInRange(dateA, dateB);
            
            double result = times.Sum(t => t.HoursAsDec());
            logger.Info($"HoursInRange, DateA: {dateA} | DateB: {dateB}.........FINISHED!!! Result: {result}");
            return result;
        }

        public override List<Time> TimesinRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"TimesInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"TimesInRange: {dateA}-{dateB}");
            List<Time> times = new List<Time>();
            string qry = $"select * from {TimeTableName}";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                if (Convert.ToDateTime(row["Date"].ToString()) >= Convert.ToDateTime(dateA)
                    && Convert.ToDateTime(row["Date"].ToString()) <= Convert.ToDateTime(dateB))
                {
                    times.Add(new Time(Convert.ToDateTime(row["TimeIn"].ToString()), Convert.ToDateTime(row["TimeOut"].ToString()))
                    {
                        Key = Convert.ToDouble(row["Key"].ToString())
                    });
                }
            }

            logger.Info($"TimesInRange, DateA: {dateA} | DateB: {dateB}.........FINISHED!!! Count: {times.Count}");
            return times;
        }
        
        public override List<Day> DaysInRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"DaysInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"DaysInRange: {dateA}-{dateB}");
            
            var times = new List<Day>();
            string qry = $"select * from {DayTableName}";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                if (Convert.ToDateTime(row["Date"].ToString()) >= Convert.ToDateTime(dateA)
                    && Convert.ToDateTime(row["Date"].ToString()) <= Convert.ToDateTime(dateB))
                {
                    times.Add(new Day(Convert.ToDateTime(row["Date"].ToString()))
                    {
                        Details = row["Details"].ToString(),
                        Times = TimesinRange(Convert.ToDateTime(row["Date"].ToString()),Convert.ToDateTime(row["Date"].ToString()))
                    });
                }
            }
            logger.Info($"DaysInRange, DateA: {dateA} | DateB: {dateB}.........FINISHED!!! Count: {times.Count}");
            return times;
        }

        public override List<Time> AllTimes()
        {
            List<Time> times = new List<Time>();
            string qry = $"select * from {TimeTableName}";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                times.Add(new Time
                {
                    TimeIn = Convert.ToDateTime(row["TimeIn"].ToString()),
                    TimeOut = Convert.ToDateTime(row["TimeOut"].ToString()),
                    Key = Convert.ToDouble(row["Key"])
                });
            }

            return times;
        }

        public override List<Day> AllDays()
        {
            List<Day> days = new List<Day>();
            string qry = $"select * from {DayTableName}";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                days.Add(new Day(Convert.ToDateTime(row["Date"].ToString()))
                {
                    Details = row["Details"].ToString(),
                    Times = TimesinRange(Convert.ToDateTime(row["Date"].ToString()),Convert.ToDateTime(row["Date"].ToString()))
                });
            }

            return days;
        }

        public override double MaxTimeId(string tablename = "")
        {
            if (tablename == "")
                tablename = TimeTableName;
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"Select Max(Key) From '{tablename}'";
                object result = cmd.ExecuteScalar();
                if (result.ToString() == "" || result is DBNull)
                    result = 0;
                
                return Math.Max(double.Parse(result.ToString()), 0);
            }   
        }
        
        public override DateTime MinDate()
        {
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"Select Min(Date) from {DayTableName}";
                object result = cmd.ExecuteScalar();
                
                if (result.ToString() == "" || result is DBNull)
                    return DateTime.MinValue;

                return Convert.ToDateTime(result.ToString());
            }
        }
        
        public override DateTime MaxDate()
        {
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"Select Max(Date) from {DayTableName}";
                object result = cmd.ExecuteScalar();
                
                if (result.ToString() == "" || result is DBNull)
                    return DateTime.MinValue;

                return Convert.ToDateTime(result.ToString());
            }
        }

        public override void RePushToServer()
        {
            logger.Info("RePushToServer........");
            logger.Info("RePushToServer........FINISHED!!!");
        }

        public override void LoadFromServer()
        {
            logger.Info("LoadFromServer.........");
            logger.Info("LoadFromServer.........FINISHED!!!");
        }

        #region sqlite specific methods

        public static Sqlite LoadFromFile()
        {
            if (Path.GetExtension(AppSettings.DataPath) != ".sqlite")
            {
                TimeData data = TimeData.Load(AppSettings.DataPath);
                string newPath = Path.ChangeExtension(AppSettings.DataPath, ".sqlite");
                
                if (newPath != null && File.Exists(newPath))
                    File.Delete(newPath);
                
                SQLiteConnection.CreateFile(newPath);
                Sqlite instance = new Sqlite("Data Source="+ newPath +";Version=3");
                
                foreach (User user in data.Users)
                {
                    instance.AddUser(user);
                }
                
                AppSettings.DataPath = newPath;
                return instance;
            }
            if (!File.Exists(AppSettings.DataPath))
            {
                SQLiteConnection.CreateFile(AppSettings.DataPath);
                
            }
            return new Sqlite("Data Source="+ AppSettings.DataPath +";Version=3");
        }

        #endregion    
    }
}