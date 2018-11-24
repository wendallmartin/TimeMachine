using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public SQLiteConnection GetConnection => _connection;

        public Sqlite(string conStringBuilder)
        {
            logger.Info("Initalize......");
            _connection = new SQLiteConnection(conStringBuilder);
            _connectionString = conStringBuilder;
            
            _connection.Open();
            
            VerifySql();

            if(AppSettings.Instance.LastVersion != Program.CurrentVersion)
                FixVersionMismatches();
            
            logger.Info("Initalize......FINISHED!!!");
        }

        public void FixVersionMismatches()
        {
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                var allDays = AllDays();
                if (allDays.Count > 0)
                {
                    try
                    {
                        Day day = allDays.FindLast(d => d.Times.Count > 0);
                        if (day.Times.Last().Key.Length < 19)// This will throw exceptions on empty data base but that is fine. We will update database in catch.
                        {
                            UpdateStringKey();
                        }
                    }
                    catch
                    {
                        UpdateStringKey();
                    }
                }
                
                void UpdateStringKey()// Changes time key to string
                {
                    cmd.CommandText = $"DROP TABLE IF EXISTS [{DayTableName}]";
                    cmd.ExecuteNonQuery();
                    
                    cmd.CommandText = $"DROP TABLE IF EXISTS [{TimeTableName}]";
                    cmd.ExecuteNonQuery();
                    
                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{DayTableName}] ( [Date] TEXT, [Details] TEXT)";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{TimeTableName}]([Date] TEXT, [TimeIn] TEXT, [TimeOut] TEXT, [Key] TEXT)";
                    cmd.ExecuteNonQuery();
                    
                    foreach (Day day in allDays)
                    {
                        day.Times.ForEach(t => t.Key = GenerateId());
                        AddDay(day);
                    }
                }
            }
        }

        public sealed override void VerifySql()
        {
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                if (AppSettings.Instance.MySqlDatabase == "")
                    AppSettings.Instance.MySqlDatabase = "TimeDataBase";

                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{GitTableName}] ([Committer] TEXT,  [Date] TEXT, [Message] TEXT, [Branch] TEXT, [Url] TEXT, Id TEXT, UNIQUE(Url, Id))";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{UserTable}]([Name] TEXT , [Rate] TEXT , [Unit] TEXT , [Active] TEXT )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{DayTableName}] ( [Date] TEXT, [Details] TEXT )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{TimeTableName}] ([Date] TEXT, [TimeIn] TEXT, [TimeOut] TEXT, [Key] TEXT )";
                cmd.ExecuteNonQuery();
            }
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
                DataTable dataTable = new DataTable();
                using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM [{UserTable}]", _connection))
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
            object timeIn;
            object timeOut;
            string lastKey = LastTimeId();
            using (SQLiteCommand selectMaxIn = new SQLiteCommand($"Select `TimeIn` From [{TimeTableName}] WHERE Key = @Key", _connection))
            {
                selectMaxIn.Parameters.Add(new SQLiteParameter("Key", lastKey));
                timeIn = selectMaxIn.ExecuteScalar();
                if (timeIn == null) return false;
                if (timeIn.ToString() == "") return false;
            }

            using (SQLiteCommand selectMaxOut = new SQLiteCommand($"Select `TimeOut` From [{TimeTableName}] WHERE Key = @Key", _connection))
            {
                selectMaxOut.Parameters.Add(new SQLiteParameter("Key", lastKey));
                timeOut = selectMaxOut.ExecuteScalar();
                if (timeOut == null) return false;
                if (timeOut.ToString() == "") return false;
            }

            return timeIn.ToString() == timeOut.ToString();
        }

        public override void AddUser(User user)
        {
            logger.Info($"Add user: {user.UserName}..........");
            Debug.WriteLine($"AddUser: {user.UserName}");
            string dayTable = ToDayTableName(user.UserName);
            string timeTable = ToTimeTableName(user.UserName);
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {                
                cmd.CommandText = $"Insert into [{UserTable}] (Name, Rate, Unit, Active) values (@Name, @Rate, @Unit, @Active)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SQLiteParameter("Name", user.UserName));
                cmd.Parameters.Add(new SQLiteParameter("Rate", 0));
                cmd.Parameters.Add(new SQLiteParameter("Unit", ""));
                cmd.Parameters.Add(new SQLiteParameter("Active", "false"));
                cmd.ExecuteNonQuery();
                                                
                foreach (Day day in user.Days)
                {
                    cmd.CommandText = $"INSERT INTO [{dayTable}] VALUES(@Date, @Details)";
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(day.Date)));
                    cmd.Parameters.Add(new SQLiteParameter("Details", day.Details));
                    cmd.ExecuteNonQuery();
                    foreach (Time time in day.Times)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = $"INSERT INTO [{timeTable}] VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                        cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(time.TimeIn.Date)));
                        cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                        cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                        cmd.Parameters.Add(new SQLiteParameter("Key", time.Key));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            logger.Info($"Add user: {user.UserName}..........FINISHED!!!");
        }

        public override Day CurrentDay()
        {
            Day day = null;
            using (SQLiteCommand cmdDay = _connection.CreateCommand())
            {
                cmdDay.CommandText = $"select `Details` from [{DayTableName}] Where Date = '{DateSqLite(DateTime.Now)}'";
                object res = cmdDay.ExecuteScalar();
                if (res != null)
                {
                    day = new Day(DateTime.Now) {Details = res.ToString()};
                    
                    // Add times to day
                    using (SQLiteCommand cmdTime = _connection.CreateCommand())
                    {
                        cmdTime.CommandText = $"select * from [{TimeTableName}] Where Date = '{DateSqLite(DateTime.Now)}'";
                        cmdTime.Parameters.Add(new SQLiteParameter());
                        using (SQLiteDataReader rdr = cmdTime.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                Time time = new Time(Convert.ToDateTime(rdr["TimeIn"].ToString()), Convert.ToDateTime(rdr["TimeOut"].ToString()));
                                day.AddTime(time);
                            }
                        }
                    }
                }
                
                if(day == null)
                {
                    day = new Day(DateTime.Now);
                    AddDay(day);
                }
            }

            return day;
        }

        public override void DeleteTime(string key)
        {
            logger.Info($"Delete time: {key}.........");
            Debug.WriteLine($"DeleteTime: {key}");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM [{TimeTableName}] WHERE Key = @Key";
                cmd.Parameters.Add(new SQLiteParameter("Key", key));
                cmd.ExecuteNonQuery();
            }
            logger.Info($"Delete time: {key}.........FINISHED!!!");
        }

        public override void AddDay(Day day)
        {
            logger.Info($"Add day: {day.Date}.........");
            Debug.WriteLine($"AddDay: {day}");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO [{DayTableName}] VALUES(@Date, @Details)";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(day.Date.Date)));
                cmd.Parameters.Add(new SQLiteParameter("Details", day.Details));
                cmd.ExecuteNonQuery();
                
                foreach (Time time in day.Times)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = $"INSERT INTO [{TimeTableName}] VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                    cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(time.TimeIn.Date)));
                    cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                    cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                    cmd.Parameters.Add(new SQLiteParameter("Key", time.Key));
                    cmd.ExecuteNonQuery();
                }
            }
            logger.Info($"Add day: {day.Date}.........FINISHED!!!");
        }


        public override void DeleteDay(DateTime date)
        {
            logger.Info($"Delete day: {date}........");
            
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM [{DayTableName}] WHERE Date = @Date";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(date)));
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $"DELETE FROM [{TimeTableName}] WHERE Date = @Date";
                cmd.ExecuteNonQuery();
             }

            logger.Info($"Delete day: {date}........FINISHED!!! ");
        }

        public override void DeleteRange(DateTime start, DateTime end)
        {
            logger.Info($"Delete range, Start: {start} | End: {end}.........");
            
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                string stringstart = DateSqLite(start);
                string stringend = DateSqLite(end);
                
                cmd.CommandText = $"DELETE FROM [{DayTableName}] WHERE Date >= @start AND Date <= @end";
                cmd.Parameters.Add(new SQLiteParameter("start", stringstart));
                cmd.Parameters.Add(new SQLiteParameter("end", stringend));
                cmd.ExecuteNonQuery();
                
                cmd.Parameters.Clear();
                
                cmd.CommandText = $"DELETE FROM [{TimeTableName}] WHERE Date >= @start AND Date <= @end";
                cmd.Parameters.Add(new SQLiteParameter("start", stringstart));
                cmd.Parameters.Add(new SQLiteParameter("end", stringend));
                cmd.ExecuteNonQuery();
            }

            logger.Info($"Delete range, Start: {start} | End: {end}..........FINISHED!!!");
        }

        public override void DeleteUser(string username)
        {
            logger.Info($"Delete user: {username}.........");
            
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"DROP TABLE IF EXISTS [{ToDayTableName(username)}]";
                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"DROP TABLE IF EXISTS [{ToTimeTableName(username)}]";
                cmd.ExecuteNonQuery();
            }
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM [{UserTable}] WHERE Name = @Name";
                cmd.Parameters.Add(new SQLiteParameter("Name", username));
                cmd.ExecuteNonQuery();
            }
            logger.Info($"Delete user: {username}.........FINISHED!!! ");
        }

        public override void UpdateDetails(DateTime date, string details)
        {
            logger.Info($"Update details, Date: {date} | Details: {details}.........");
            
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"UPDATE [{DayTableName}] SET Details = @Details WHERE Date = '{DateSqLite(date.Date)}'";
                cmd.Parameters.Add(new SQLiteParameter("Details", details));
                cmd.ExecuteNonQuery();
            }

            logger.Info($"Update details, Date: {date} | Details: {details}.........FINISHED!!! ");
        }

        public override void UpdateTime(string key, Time upd)
        {
            logger.Info($"Update time, Key: {key} | Update: {upd}...........");
            
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"select * from [{TimeTableName}] Where Date = @Date";
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
                cmd.CommandText = $"UPDATE [{TimeTableName}] SET Date = @Date, TimeIn = @TimeIn, TimeOut = @TimeOut WHERE Key = @Key";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(upd.TimeIn.Date)));
                cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(upd.TimeIn)));
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(upd.TimeOut)));
                cmd.Parameters.Add(new SQLiteParameter("Key", key));
                cmd.ExecuteNonQuery();
            }
            
            logger.Info($"Update time, Key: {key} | Update: {upd}...........FINISHED!!! ");
        }

        public override void PunchIn(string key)
        {
            logger.Info($"Punchin: {key}");
            Debug.WriteLine("PunchIn");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {                
                DateTime now = DateTime.Now;
                cmd.CommandText = $"INSERT INTO [{TimeTableName}] VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(now.Date)));
                cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(now)));
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(now)));
                cmd.Parameters.Add(new SQLiteParameter("Key", key));
                cmd.ExecuteNonQuery();
            }
        }

        public override void PunchOut(string key)
        {
            logger.Info("PunchOut");
            Debug.WriteLine("PunchOut");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"UPDATE [{TimeTableName}] SET TimeOut = @TimeOut WHERE Key = '{key}'";
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(DateTime.Now)));
                cmd.ExecuteNonQuery();
            }
        }

        public override TimeSpan HoursInRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"HoursInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"HoursInRange: {dateA}-{dateB}");
            var days = DaysInRange(dateA, dateB);
            
            TimeSpan result = new TimeSpan();
            foreach (Day day in days)
            {
                result = result.Add(day.Hours());
            }
            
            logger.Info($"HoursInRange, DateA: {dateA} | DateB: {dateB}.........FINISHED!!! Result: {result}");
            return result;
        }

        public override List<Time> TimesinRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"TimesInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"TimesInRange: {dateA}-{dateB}");
            List<Time> times = new List<Time>();
            string qry = $"select * from [{TimeTableName}]";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                try
                {
                    if (Convert.ToDateTime(row["Date"].ToString()) >= Convert.ToDateTime(dateA)
                        && Convert.ToDateTime(row["Date"].ToString()) <= Convert.ToDateTime(dateB))
                    {
                        DateTime.TryParse(row["TimeIn"].ToString(), out DateTime inTime);
                        DateTime.TryParse(row["TimeOut"].ToString(), out DateTime outTime);
                        string key = row["Key"].ToString(); 
                        times.Add(new Time(inTime, outTime){ Key = key });
                    }
                }
                catch (Exception e)
                {
                    logger.Info($"\n\n----------------SQLite TimesInRange EXCEPTION!!! (Date: {row["Date"]} TimeIn: {row["TimeIn"]} TimeOut: {row["TimeOut"]})----------------\n" + e);   
                }
            }

            logger.Info($"TimesInRange, DateA: {dateA} | DateB: {dateB}.........FINISHED!!! Count: {times.Count}");
            return times;
        }
        
        public override List<Day> DaysInRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"DaysInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"DaysInRange: {dateA}-{dateB}");
            
            var days = new List<Day>();
            string qry = $"select * from [{DayTableName}]";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                try
                {
                    if (Convert.ToDateTime(row["Date"].ToString()) >= Convert.ToDateTime(dateA)
                        && Convert.ToDateTime(row["Date"].ToString()) <= Convert.ToDateTime(dateB))
                    {
                        days.Add(new Day(Convert.ToDateTime(row["Date"].ToString()))
                        {
                            Details = row["Details"].ToString(),
                            Times = TimesinRange(Convert.ToDateTime(row["Date"].ToString()),Convert.ToDateTime(row["Date"].ToString()))
                        });
                    }
                }
                catch (Exception e)
                {
                    logger.Info($"\n\n----------------SQLite TimesInRange EXCEPTION!!! (Date: {row["Date"]} TimeIn: {row["TimeIn"].ToString()} TimeOut: {row["TimeOut"]})----------------\n" + e);
                }
            }
            logger.Info($"DaysInRange, DateA: {dateA} | DateB: {dateB}.........FINISHED!!! Count: {days.Count}");
            return days;
        }

        public override List<Time> AllTimes()
        {
            List<Time> times = new List<Time>();
            string qry = $"select * from [{TimeTableName}]";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                times.Add(new Time
                {
                    TimeIn = Convert.ToDateTime(row["TimeIn"].ToString()),
                    TimeOut = Convert.ToDateTime(row["TimeOut"].ToString()),
                    Key = row["Key"].ToString()
                });
            }

            return times;
        }

        public override List<Day> AllDays()
        {
            List<Day> days = new List<Day>();
            string qry = $"select * from [{DayTableName}]";
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

        public override string LastTimeId()
        {
            string qry = $"select * from [{TimeTableName}]";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            // Return last row id.
            return vt.Rows.Count > 0 ? vt.Rows[vt.Rows.Count - 1]["Key"].ToString() : "0";
        }
        
        public override DateTime MinDate()
        {
            var times = AllDays();
            return times.Min(t => t.Date);
        }
        
        public override DateTime MaxDate()
        {
            var times = AllDays();
            return times.Max(t => t.Date);
        }

        public override void Push(List<Day> days)
        {
            logger.Info($"Repushing to server.........count = {days.Count()}");
            float totalDays = days.Count;
            float completedDays = 0;

            days = days.OrderBy(d => d.Date).ToList();
            
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                foreach (Day day in days)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = $"DELETE FROM [{DayTableName}] WHERE Date = @Date";
                    cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(day.Date)));
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = $"DELETE FROM [{TimeTableName}] WHERE Date = @Date";
                    cmd.ExecuteNonQuery();
                }
                
                foreach (Day day in days)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = $"INSERT INTO [{DayTableName}] VALUES(@Date, @Details)";
                    cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(day.Date.Date)));
                    cmd.Parameters.Add(new SQLiteParameter("Details", day.Details));
                    cmd.ExecuteNonQuery();

                    foreach (Time time in day.Times)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = $"INSERT INTO [{TimeTableName}] VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                        cmd.Parameters.Add(new SQLiteParameter("Date", DateSqLite(time.TimeIn.Date)));
                        cmd.Parameters.Add(new SQLiteParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                        cmd.Parameters.Add(new SQLiteParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                        cmd.Parameters.Add(new SQLiteParameter("Key", time.Key));
                        cmd.ExecuteNonQuery();
                    }
                    
                    ProgressChangedEvent?.Invoke(completedDays++ / totalDays * 100f);
                }
            }
            ProgressFinishEvent?.Invoke();
            logger.Info($"Repushing to server.........count = {days.Count()} FINISHED!!!");
        }

        public override List<Day> Pull()
        {
            logger.Info("LoadFromServer");
            return DaysInRange(DateTime.MinValue, DateTime.MaxValue);
        }

        #region sqlite specific methods

        public static Sqlite LoadFromFile()
        {
            if (Path.GetExtension(AppSettings.Instance.DataPath) != ".sqlite")
            {
                TimeData data = TimeData.Load(AppSettings.Instance.DataPath);
                string newPath = Path.ChangeExtension(AppSettings.Instance.DataPath, ".sqlite");
                
                if (newPath != null && File.Exists(newPath))
                    File.Delete(newPath);
                
                SQLiteConnection.CreateFile(newPath);
                Sqlite instance = new Sqlite("Data Source="+ newPath +";Version=3");

                if (data.Users == null || data.Users.Count == 0)
                {
                    EnterUser enterUser = new EnterUser();
                    enterUser.ShowDialog();
                    List<Day> days = new List<Day>();
                    if (data.Days != null && data.Days.Count > 0)
                    {
                        days = data.Days;
                    }
                    
                    User user = new User(enterUser.UserText, "", days);
                    instance.AddUser(user);
                }
                else
                {
                    foreach (User user in data.Users)
                    {
                        instance.AddUser(user);
                    }                    
                }
                
                AppSettings.Instance.DataPath = newPath;
                return instance;
            }
            if (!File.Exists(AppSettings.Instance.DataPath))
            {
                SQLiteConnection.CreateFile(Path.ChangeExtension(AppSettings.Instance.DataPath, ".sqlite"));
                
            }
            return new Sqlite("Data Source="+ AppSettings.Instance.DataPath +";Version=3");
        }

        #endregion

        #region git support

        public override void AddCommit(GitCommit commit)
        {
            logger.Info($"Add commit: {commit.Message}");
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"INSERT OR IGNORE INTO [{GitTableName}] VALUES(@Committer, @Date, @Message, @Branch, @Url, @Id)";
                cmd.Parameters.Add(new SQLiteParameter("Committer", commit.Committer));
                cmd.Parameters.Add(new SQLiteParameter("Date", DateTimeSqLite(commit.Date)));
                cmd.Parameters.Add(new SQLiteParameter("Message", commit.Message));
                cmd.Parameters.Add(new SQLiteParameter("Branch", commit.Branch));
                cmd.Parameters.Add(new SQLiteParameter("Url", commit.Url));
                cmd.Parameters.Add(new SQLiteParameter("Id", commit.Id));
                cmd.ExecuteNonQuery();
            }
        }

        public override List<GitCommit> GetCommits(DateTime datetime)
        {
            logger.Info($"Get commits on date: {datetime}");
            return GetCommits().Where(c => c.Date.Date == datetime.Date).ToList();
        }

        public override List<GitCommit> GetCommits()
        {
            logger.Info("Get Commits...........");
            List<GitCommit> result = new List<GitCommit>();
            string qry = $"SELECT * FROM [{GitTableName}]";
            DataTable vt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                GitCommit commit = new GitCommit(row["Committer"].ToString(), Convert.ToDateTime(row["Date"].ToString()), row["Message"].ToString(), row["Id"].ToString()) {Branch = row["Branch"].ToString(), Url = row["Url"].ToString(),};
                result.Add(commit);
            }

            logger.Info("Get Commits...........FINISHED!!!");
            return result;
        }

        public override void RemoveCommit(GitCommit commit)
        {
            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM [{GitTableName}] WHERE (Committer = @Committer AND Date = @Date AND Message = @Message AND Branch = @Branch AND Url = @Url AND Id = @Id)";
                cmd.Parameters.Add(new SQLiteParameter("Committer", commit.Committer));
                cmd.Parameters.Add(new SQLiteParameter("Date", DateTimeSqLite(commit.Date)));
                cmd.Parameters.Add(new SQLiteParameter("Message", commit.Message));
                cmd.Parameters.Add(new SQLiteParameter("Branch", commit.Branch));
                cmd.Parameters.Add(new SQLiteParameter("Url", commit.Url));
                cmd.Parameters.Add(new SQLiteParameter("Id", commit.Id));
                cmd.ExecuteNonQuery();
            }
        }

        #endregion
    }
}