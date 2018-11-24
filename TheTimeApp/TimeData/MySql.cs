using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MySql.Data.MySqlClient;
using NLog;
using MySqlDataAdapter = MySql.Data.MySqlClient.MySqlDataAdapter;

namespace TheTimeApp.TimeData
{
    public class MySql:TimeServer
    {
        public enum UpdateModes
        {
            Async,
            Sync
        }
        private static Logger logger = LogManager.GetCurrentClassLogger();
         
        private MySqlConnectionStringBuilder _connectionStringBuilder;
        private UpdateModes UpdateMode { get; }
        private readonly SqlBuffer _buffer;

        private bool _settup;

        private MySqlConnection _connection
        {
            get{
                var con = new MySqlConnection(_connectionStringBuilder.ConnectionString);
                try
                {
                    con.Open();
                }
                catch
                {
                    // eat em up!
                }
                
                ConnectionChangedEvent?.Invoke(con.State == ConnectionState.Open);
                return con;
            }
        } 

        public MySql(MySqlConnectionStringBuilder conStringBuilderBuilder, UpdateModes mode)
        {
            logger.Info("Initualize.........");
            UpdateMode = mode;
            _connectionStringBuilder = conStringBuilderBuilder;
            
            _buffer = SqlBuffer.Load();
            
            if (AppSettings.Instance.MySqlDatabase == "")
                AppSettings.Instance.MySqlDatabase = "TimeDataBase";

            VerifySql();

            FixVersionMismatches();
            
            logger.Info("Initualize.........FINISHED!!!");
        }

        private void FixVersionMismatches()
        {
                            
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                var allDays = AllDays();
                try
                {
                    Day day = allDays.FindLast(d => d.Times.Count > 0);
                    if (day.Times.Last().Key.Length < 19) // This will throw exceptions on empty data base but that is fine. We will update database in catch.
                    {
                        UpdateStringKey();
                    }
                }
                catch
                {
                    UpdateStringKey();
                }

                void UpdateStringKey() // Changes time key to string
                {
                    cmd.CommandText = $"DROP TABLE IF EXISTS `{DayTableName}`";
                    cmd.ExecuteNonQuery();
                    
                    cmd.CommandText = $"DROP TABLE IF EXISTS `{TimeTableName}`";
                    cmd.ExecuteNonQuery();
                    
                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{DayTableName}` ( `Date` TEXT, `Details` TEXT)";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = $"CREATE TABLE `{TimeTableName}`(`Date` TEXT, `TimeIn` TEXT, `TimeOut` TEXT, `Key` TEXT)";
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
            try
            {
                if (ServerState != State.Connected) return;
                
                using (MySqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999";
                    cmd.ExecuteNonQuery();
                }

                using (MySqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{AppSettings.Instance.MySqlDatabase}`";
                    cmd.ExecuteNonQuery();
                }

                _connectionStringBuilder.Database = AppSettings.Instance.MySqlDatabase; // only place the database is set.
                
                using (MySqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{GitTableName}` (`Committer` TEXT, `Date` TEXT, `Message` TEXT, `Branch` Text, `Url` VARCHAR(150), `Id` VARCHAR(100), UNIQUE (`Url`, `Id`))";
                    cmd.ExecuteNonQuery();
                }

                using (MySqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{UserTable}`(`Name` TEXT , `Rate` TEXT , `Unit` TEXT , `Active` TEXT )";
                    cmd.ExecuteNonQuery();
                }

                using (MySqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{DayTableName}` ( `Date` TEXT, `Details` TEXT )";
                    cmd.ExecuteNonQuery();
                }

                using (MySqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{TimeTableName}` (`Date` TEXT, `TimeIn` TEXT, `TimeOut` TEXT, `Key` TEXT )";
                    cmd.ExecuteNonQuery();
                }

                _settup = true;
            }
            catch (Exception e)
            {
                logger.Info(e.ToString());
                _settup = false;
            }
        }

        /// <summary>
        /// Saves underlying SqlBuffer to disk.
        /// </summary>
        public void SaveBuffer() => _buffer.Save();

        public List<SerilizeSqlCommand> CommandBuffer() => _buffer.Buffer(); 

        /// <summary>
        /// Clears the sql buffer
        /// but does not save to disk.
        /// </summary>
        public void ClearBuffer() => _buffer.ClearBuffer(); 
        
        private void AddToPump(MySqlCommand command)
        {
            SerilizeSqlCommand serializedSqlCommand = new SerilizeSqlCommand(command.CommandText);
            serializedSqlCommand.Type = SerilizeSqlCommand.SqlType.MySql;
            foreach (MySqlParameter commandParameter in command.Parameters)
            {
                serializedSqlCommand.AddParameter(commandParameter);
            }
            _buffer.Add(serializedSqlCommand);
            
            UpdateChangedEvent?.Invoke(_buffer.Buffer());

            if (ServerState == State.Connected)
            {
                if (UpdateMode == UpdateModes.Async) PumpSqlAsync(_buffer.Buffer());
                else PumpSql(_buffer.Buffer());    
            }
        }

        private void PumpSqlAsync(List<SerilizeSqlCommand> cmds)
        {
            new Thread(() => PumpSql(cmds)).Start();
        }

        private void PumpSql(List<SerilizeSqlCommand> commands)
        {
            if (!_settup) return; // if the current database is not setup, don't execute
        
            try
            {
                foreach (SerilizeSqlCommand command in commands)
                {
                    MySqlCommand c = command.GetMySqlCommand;
                    c.Connection = _connection;
                    c.ExecuteNonQuery();
                    _buffer.Remove(command);
                }
            }
            catch (Exception e)
            {
                logger.Info("\n\n----------------MYSLQ PUMP EXCEPTION!!!----------------\n" + e);
                Debug.WriteLine(e);
            }
            UpdateChangedEvent?.Invoke(_buffer.Buffer());
        }

        public override bool IsClockedIn()
        {
            Debug.WriteLine("IsClockedIn:");
            object timein;
            object timeout;
            string lastKey = LastTimeId();
            
            using(MySqlCommand selectMaxIn = new MySqlCommand($"Select `TimeIn` From `{TimeTableName}` WHERE `Key` = @Key", _connection))
            {
                selectMaxIn.Parameters.Add(new MySqlParameter("Key", lastKey));
                timein = selectMaxIn.ExecuteScalar();
                if (timein == null) return false;
                if (timein.ToString() == "")
                {
                    Debug.Write(" false");
                    return false;
                }
            }

            using (MySqlCommand selectMaxOut = new MySqlCommand($"Select `TimeOut` From `{TimeTableName}` WHERE `Key` = @Key", _connection))
            {
                selectMaxOut.Parameters.Add(new MySqlParameter("Key", lastKey));
                timeout = selectMaxOut.ExecuteScalar();
                if (timeout == null) return false;
                if (timeout.ToString() == "")
                {
                    Debug.Write(" false");
                    return false;
                }
            }
            
            Debug.Write(timein.ToString() == timeout.ToString());
            return timein.ToString() == timeout.ToString();
        }

        public override void AddUser(User user)
        {
            logger.Info($"Add user: {user.UserName}..........");
            Debug.WriteLine($"AddUser: {user.UserName}");
            string dayTable = ToDayTableName(user.UserName);
            string timeTable = ToTimeTableName(user.UserName);
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = $"Insert into `{UserTable}` (Name, Rate, Unit, Active) values (@Name, @Rate, @Unit, @Active)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new MySqlParameter("Name", user.UserName));
                cmd.Parameters.Add(new MySqlParameter("Rate", 0));
                cmd.Parameters.Add(new MySqlParameter("Unit", ""));
                cmd.Parameters.Add(new MySqlParameter("Active", "false"));
                AddToPump(cmd);
                
                foreach (Day day in user.Days)
                {
                    cmd.CommandText = $"INSERT INTO `{dayTable}` VALUES(@Date, @Details)";
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(day.Date)));
                    cmd.Parameters.Add(new MySqlParameter("Details", day.Details));
                    AddToPump(cmd);
                    foreach (Time time in day.Times)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = $"INSERT INTO `{timeTable}` VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                        cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(time.TimeIn.Date)));
                        cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                        cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                        cmd.Parameters.Add(new MySqlParameter("Key", time.Key));
                        AddToPump(cmd);
                    }
                }
            }
            logger.Info($"Add user: {user.UserName}..........FINISHED!!!");

        }

        public override List<string> UserNames()
        {
            Debug.WriteLine("Usernames");
            List<string> usernames = new List<string>();
            try
            {
                DataTable dataTable = new DataTable();
                using (MySqlCommand command = new MySqlCommand($"SELECT * FROM `{UserTable}`", _connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
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
        

        public override void DeleteUser(string username)
        {
            logger.Info($"Delete user: {username}.........");
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = $@"DROP TABLE IF EXISTS `{ToDayTableName(username)}`";
                AddToPump(cmd);
                cmd.CommandText = $@"DROP TABLE IF EXISTS `{ToTimeTableName(username)}`";
                AddToPump(cmd);
                cmd.CommandText = $"DELETE FROM `{UserTable}` WHERE `Name` = @Name";
                cmd.Parameters.Add(new MySqlParameter("Name", username));
                AddToPump(cmd);
            }
            logger.Info($"Delete user: {username}.........FINISHED!!!");
        }

        public override void AddDay(Day day)
        {
            logger.Info($"Add day: {day.Date}.........");
            
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = $"INSERT IGNORE INTO `{DayTableName}` VALUES(@Date, @Details)";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(day.Date.Date)));
                cmd.Parameters.Add(new MySqlParameter("Details", day.Details));
                AddToPump(cmd);
                
                foreach (Time time in day.Times)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = $"INSERT IGNORE INTO `{TimeTableName}` VALUES(@Date, @TimeIn, @TimeOut)";
                    cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(time.TimeIn.Date)));
                    cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                    cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                    AddToPump(cmd);
                }
            }
            logger.Info($"Add day: {day.Date}.........FINISHED!!!");

        }

        public override List<Day> DaysInRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"DaysInRange, DateA: {dateA} | DateB: {dateB}.........");
            
            var times = new List<Day>();
            string qry = $"Select * From `{DayTableName}`";
            DataTable vt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(qry, _connection);
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

        public override void DeleteDay(DateTime date)
        {
            logger.Info($"Delete day: {date}........");
            
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = $"DELETE FROM `{DayTableName}` WHERE Date = @Date";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(date)));
                AddToPump(cmd);
                
                cmd.CommandText = $"DELETE FROM `{TimeTableName}` WHERE Date = @Date";
                AddToPump(cmd);
            }

            logger.Info($"Delete day: {date}........FINISHED!!! ");
        }

        public override List<Day> AllDays()
        {
            List<Day> days = new List<Day>();
            string qry = $"select * from `{DayTableName}`";
            DataTable vt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(qry, _connection);
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

        public override void DeleteRange(DateTime start, DateTime end)
        {
            logger.Info($"Delete range, Start: {start} | End: {end}.........");

            using (MySqlCommand cmd = new MySqlCommand())
            {
                string stringstart = DateSqLite(start);
                string stringend = DateSqLite(end);
                
                cmd.CommandText = $"DELETE FROM `{DayTableName}` WHERE Date >= @start AND Date <= @end";
                cmd.Parameters.Add(new MySqlParameter("start", stringstart));
                cmd.Parameters.Add(new MySqlParameter("end", stringend));
                AddToPump(cmd);
                
                cmd.Parameters.Clear();
                
                cmd.CommandText = $"DELETE FROM `{TimeTableName}` WHERE Date >= @start AND Date <= @end";
                cmd.Parameters.Add(new MySqlParameter("start", stringstart));
                cmd.Parameters.Add(new MySqlParameter("end", stringend));
                AddToPump(cmd);
            }
        }

        public override void PunchIn(string key)
        {
            logger.Info($"Punchin: {key}");
            Debug.WriteLine("PunchIn");
            using (MySqlCommand cmd = new MySqlCommand())
            {
                DateTime now = DateTime.Now;
                cmd.CommandText = $"INSERT INTO `{TimeTableName}` VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(now.Date)));
                cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(now)));
                cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(now)));
                cmd.Parameters.Add(new MySqlParameter("Key", key));
                AddToPump(cmd);
            }
        }

        public override void PunchOut(string key)
        {
            logger.Info("PunchOut");
            Debug.WriteLine("PunchOut");
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = $"UPDATE `{TimeTableName}` SET `TimeOut` = @TimeOut WHERE `Key` = '{key}'";
                cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(DateTime.Now)));
                AddToPump(cmd);
            }

        }

        public override List<Time> AllTimes()
        {
            List<Time> times = new List<Time>();
            string qry = $"select * from `{TimeTableName}`";
            DataTable vt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(qry, _connection);
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

        public override Day CurrentDay()
        {
            Day day = new Day(DateTime.MinValue);
            
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"select * from `{DayTableName}` Where Date = '{DateSqLite(DateTime.Now)}'";
                cmd.Parameters.Add(new MySqlParameter());
                using (MySqlDataReader rdr = cmd.ExecuteReader())
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
            
            return day;

        }

        public override void DeleteTime(string key)
        {
            logger.Info($"Delete time: {key}.........");
            
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = $"DELETE FROM `{TimeTableName}` WHERE `Key` = @Key";
                cmd.Parameters.Add(new MySqlParameter("Key", key));
                AddToPump(cmd);
            }
            logger.Info($"Delete time: {key}.........FINISHED!!!");
        }

        /// <summary>
        /// Updates day details, creating
        /// new day if not exists.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="details"></param>
        public override void UpdateDetails(DateTime date, string details)
        {
            logger.Info($"Update details, Date: {date} | Details: {details}.........");

            try
            {
                using (MySqlCommand cmd = _connection.CreateCommand())// Add day if not exists.
                {
                    cmd.CommandText = $"SELECT Date FROM `{DayTableName}` WHERE Date = @Date";
                    cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(date.Date)));
                    if (cmd.ExecuteScalar() == null) AddDay(new Day(date.Date));
                }
            }
            catch {/* eat it! */}
            
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = $@"UPDATE `{DayTableName}` SET Details = @Details WHERE Date = @Date";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(date.Date)));
                cmd.Parameters.Add(new MySqlParameter("Details", details));
                AddToPump(cmd);
            }

            logger.Info($"Update details, Date: {date} | Details: {details}.........FINISHED!!! ");
        }

        /// <summary>
        /// Updates time, creating
        /// new day if not exists
        /// </summary>
        /// <param name="key"></param>
        /// <param name="upd"></param>
        public override void UpdateTime(string key, Time upd)
        {
            logger.Info($"Update time, Key: {key} | Update: {upd}...........");

            try
            {
                using (MySqlCommand cmd = _connection.CreateCommand())// Add day if not exists.
                {
                    cmd.CommandText = $"Select Date from `{DayTableName}` Where Date = @Date";
                    cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(upd.TimeIn.Date)));
                    if (cmd.ExecuteScalar() == null) AddDay(new Day(upd.TimeIn.Date));
                }                
            }
            catch {/* eat it! */}

            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = $"UPDATE `{TimeTableName}` SET `Date` = @Date, `TimeIn` = @TimeIn, `TimeOut` = @TimeOut WHERE `Key` = @Key";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(upd.TimeIn.Date)));
                cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(upd.TimeIn)));
                cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(upd.TimeOut)));
                cmd.Parameters.Add(new MySqlParameter("Key", key));
                AddToPump(cmd);
            }
            
            logger.Info($"Update time, Key: {key} | Update: {upd}...........FINISHED!!! ");
        }

        public override string LastTimeId()
        {
            string qry = $"select * from `{TimeTableName}`";
            DataTable vt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(qry, _connection);
            da.Fill(vt);

            return vt.Rows.Count > 0 ? vt.Rows[vt.Rows.Count - 1]["Key"].ToString() : "0";
        }

        public override List<Time> TimesinRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"TimesInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"TimesInRange: {dateA}-{dateB}");
            List<Time> times = new List<Time>();
            string qry = $"select * from `{TimeTableName}`";
            DataTable vt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                if (Convert.ToDateTime(row["Date"].ToString()) >= dateA
                    && Convert.ToDateTime(row["Date"].ToString()) <= dateB)
                {
                    DateTime inTime = Convert.ToDateTime(row["TimeIn"].ToString());
                    DateTime outTime = Convert.ToDateTime(row["TimeOut"].ToString());
                    times.Add(new Time(inTime, outTime)
                    {
                        Key = row["Key"].ToString()
                    });
                }
            }

            logger.Info($"TimesInRange, DateA: {dateA} | DateB: {dateB}.........FINISHED!!! Count: {times.Count}");
            return times;

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

        
        #region git support

        public override void AddCommit(GitCommit commit)
        {
            logger.Info($"Add commit: {commit.Message}..........");
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"INSERT IGNORE INTO `{GitTableName}` VALUES(@Committer, @Date, @Message, @Branch, @Url, @Id)";
                cmd.Parameters.Add(new MySqlParameter("Committer", commit.Committer));
                cmd.Parameters.Add(new MySqlParameter("Date", DateTimeSqLite(commit.Date)));
                cmd.Parameters.Add(new MySqlParameter("Message", commit.Message));
                cmd.Parameters.Add(new MySqlParameter("Branch", commit.Branch));
                cmd.Parameters.Add(new MySqlParameter("Url", commit.Url));
                cmd.Parameters.Add(new MySqlParameter("Id", commit.Id));
                AddToPump(cmd);
            }
            logger.Info($"Add commit: {commit.Message}..........FINISHED!!!");
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
            string qry = $"SELECT * FROM `{GitTableName}`";
            DataTable vt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(qry, _connection);
            da.Fill(vt);

            foreach (DataRow row in vt.Rows)
            {
                GitCommit commit = new GitCommit(row["Committer"].ToString(), Convert.ToDateTime(row["Date"].ToString()), row["Message"].ToString(), row["Id"].ToString())
                {
                    Branch = row["Branch"].ToString(),
                    Url = row["Url"].ToString()
                };
                result.Add(commit);
            }

            logger.Info("Get Commits...........FINISHED!!!");
            return result;
        }

        public override void RemoveCommit(GitCommit commit)
        {
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM `{GitTableName}` WHERE (`Committer` = @Committer AND `Date` = @Date AND `Message` = @Message AND `Branch` = @Branch AND `Url` = @Url AND `Id` = @Id)";
                cmd.Parameters.Add(new MySqlParameter("Committer", commit.Committer));
                cmd.Parameters.Add(new MySqlParameter("Date", DateTimeSqLite(commit.Date)));
                cmd.Parameters.Add(new MySqlParameter("Message", commit.Message));
                cmd.Parameters.Add(new MySqlParameter("Branch", commit.Branch));
                cmd.Parameters.Add(new MySqlParameter("Url", commit.Url));
                cmd.Parameters.Add(new MySqlParameter("Id", commit.Id));
                AddToPump(cmd);
            }
        }

        #endregion

        
        public override void Push(List<Day> days)
        {
            logger.Info($"Repushing to server.........count = {days.Count()}");

            if (ServerState != State.Connected) throw new Exception("Not Connected!");
            
            float totalDays = days.Count;
            float completedDays = 0;
            
            days = days.OrderBy(d => d.Date).ToList();
            
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                try
                {
                    foreach (Day day in days)
                    {
                        // remove day
                        cmd.Parameters.Clear();
                        cmd.CommandText = $"DELETE FROM `{DayTableName}` WHERE Date = @Date";
                        cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(day.Date)));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = $"DELETE FROM `{TimeTableName}` WHERE Date = @Date";
                        cmd.ExecuteNonQuery();
                    }
                    
                    foreach (Day day in days) // insert all days and times in range
                    {
                        // re-push day
                        cmd.Parameters.Clear();
                        cmd.CommandText = $"INSERT INTO `{DayTableName}` VALUES(@Date, @Details)";
                        cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(day.Date.Date)));
                        cmd.Parameters.Add(new MySqlParameter("Details", day.Details));
                        cmd.ExecuteNonQuery();

                        foreach (Time time in day.Times)
                        {
                            {
                                cmd.Parameters.Clear();
                                cmd.CommandText = $"INSERT INTO `{TimeTableName}` VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(time.TimeIn.Date)));
                                cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                                cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                                cmd.Parameters.Add(new MySqlParameter("Key", time.Key));
                                cmd.ExecuteNonQuery();
                            }
                        }
                        
                        ProgressChangedEvent?.Invoke(completedDays++ / totalDays * 100f);
                    }
                    ProgressFinishEvent?.Invoke();
                }
                catch (Exception e)
                {
                    logger.Info("\n\n----------------MYSLQ RePushToServer EXCEPTION!!!----------------\n" + e);
                }
            }

            logger.Info($"Repushing to server.........count = {days.Count()} FINISHED!!!");
        }

        public override List<Day> Pull()
        {
            if (ServerState != State.Connected) throw new Exception("Not Connected!");
            return DaysInRange(DateTime.MinValue, DateTime.MaxValue);
        }

        public override void Dispose()
        {
            logger.Info("Disposing......");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            logger.Info("Disposing......FINISHED!!!");
        }

        public void ClearDataBase(string databasename)
        {
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DROP DATABASE IF EXISTS `{databasename}`";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"CREATE DATABASE `{databasename}`";
                cmd.ExecuteNonQuery();
            }
        }
    }
}