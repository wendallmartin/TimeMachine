using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using MySql.Data.MySqlClient;
using NLog;
using MySqlDataAdapter = MySql.Data.MySqlClient.MySqlDataAdapter;

namespace TheTimeApp.TimeData
{
    public class MySql:TimeServer
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private MySqlConnection _connection;
        public string DataBase { get; set; } = "testsql";

        private readonly string _connectionString;
        
        public MySql(string conStringBuilder)
        {
            logger.Info("Initualize.........");
            _connectionString = conStringBuilder;
            _connection = new MySqlConnection(conStringBuilder);
            _connection.Open();
            
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                if (AppSettings.Instance.MySqlDatabase == "")
                    AppSettings.Instance.MySqlDatabase = "TimeDataBase";
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{AppSettings.Instance.MySqlDatabase}`";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{UserTable}`(`Name` TEXT , `Rate` TEXT , `Unit` TEXT , `Active` TEXT )";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS`{DayTableName}` ( `Date` TEXT, `Details` TEXT )";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{TimeTableName}` (`Date` TEXT, `TimeIn` TEXT, `TimeOut` TEXT, `Key` INT )";
                cmd.ExecuteNonQuery();
            }
            
            logger.Info("Initualize.........FINISHED!!!");
        } 
        
        public override bool IsClockedIn()
        {
            Debug.WriteLine("IsClockedIn:");
            object timein;
            object timeout;

            using(MySqlCommand selectMaxIn = new MySqlCommand($"Select Max(TimeIn) From {TimeTableName}", _connection))
            {
                timein = selectMaxIn.ExecuteScalar();
                if (timein.ToString() == "")
                {
                    Debug.Write(" false");
                    return false;
                }
            }

            using (MySqlCommand selectMaxOut = new MySqlCommand($"Select Max(TimeOut) From {TimeTableName}", _connection))
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

        public override void AddUser(User user)
        {
            logger.Info($"Add user: {user.UserName}..........");
            Debug.WriteLine($"AddUser: {user.UserName}");
            string dayTable = ToDayTableName(user.UserName);
            string timeTable = ToTimeTableName(user.UserName);
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{UserTable}`(`Name` TEXT , `Rate` TEXT , `Unit` TEXT , `Active` TEXT )";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $"Insert into `{UserTable}` (Name, Rate, Unit, Active) values (@Name, @Rate, @Unit, @Active)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new MySqlParameter("Name", user.UserName));
                cmd.Parameters.Add(new MySqlParameter("Rate", 0));
                cmd.Parameters.Add(new MySqlParameter("Unit", ""));
                cmd.Parameters.Add(new MySqlParameter("Active", "false"));
                cmd.ExecuteNonQuery();
                
                
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{dayTable}` ( `Date` TEXT, `Details` TEXT )";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{timeTable}` (`Date` TEXT, `TimeIn` TEXT, `TimeOut` TEXT, `Key` INT )";
                cmd.ExecuteNonQuery();
                foreach (Day day in user.Days)
                {
                    cmd.CommandText = $"INSERT INTO `{dayTable}` VALUES(@Date, @Details)";
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(day.Date)));
                    cmd.Parameters.Add(new MySqlParameter("Details", day.Details));
                    cmd.ExecuteNonQuery();
                    foreach (Time time in day.Times)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = $"INSERT INTO `{timeTable}` VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                        cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(time.TimeIn.Date)));
                        cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                        cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                        cmd.Parameters.Add(new MySqlParameter("Key", MaxTimeId(timeTable) + 1));
                        cmd.ExecuteNonQuery();
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
                using (MySqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{UserTable}` ( `Name` TEXT, `Rate` TEXT, `Unit` TEXT, `Active` TEXT )";
                    cmd.ExecuteNonQuery();
                }

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

        public override int DeleteUser(string username)
        {
            logger.Info($"Delete user: {username}.........");
            int result = 0;
            Debug.WriteLine($"DeleteUser: {username}");
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"DROP TABLE IF EXISTS `{ToDayTableName(username)}`";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $@"DROP TABLE IF EXISTS `{ToTimeTableName(username)}`";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"DELETE FROM `{UserTable}` WHERE `Name` = @Name";
                cmd.Parameters.Add(new MySqlParameter("Name", username));
                result = cmd.ExecuteNonQuery();
            }
            logger.Info($"Delete user: {username}.........FINISHED!!! Result: {result}");
            return result;

        }

        public override void AddDay(Day day)
        {
            logger.Info($"Add day: {day.Date}.........");
            Debug.WriteLine($"AddDay: {day}");
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {DayTableName} VALUES(@Date, @Details)";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(day.Date.Date)));
                cmd.Parameters.Add(new MySqlParameter("Details", day.Details));
                cmd.ExecuteNonQuery();
                
                foreach (Time time in day.Times)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = $"INSERT INTO {TimeTableName} VALUES(@Date, @TimeIn, @TimeOut)";
                    cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(time.TimeIn.Date)));
                    cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(time.TimeIn)));
                    cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(time.TimeOut)));
                    cmd.ExecuteNonQuery();
                }
            }
            logger.Info($"Add day: {day.Date}.........FINISHED!!!");

        }

        public override List<Day> DaysInRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"DaysInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"DaysInRange: {dateA}-{dateB}");
            
            var times = new List<Day>();
            string qry = $"select * from {DayTableName}";
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

        public override int DeleteDay(DateTime date)
        {
            logger.Info($"Delete day: {date}........");
            Debug.WriteLine($"DeleteDay: {date}");
            int result = 0;
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {DayTableName} WHERE Date = @Date";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(date)));
                result += cmd.ExecuteNonQuery();
                
                cmd.CommandText = $"DELETE FROM {TimeTableName} WHERE Date = @Date";
                result += cmd.ExecuteNonQuery();
            }

            logger.Info($"Delete day: {date}........FINISHED!!! Result: {result}");
            return result;

        }

        public override List<Day> AllDays()
        {
            List<Day> days = new List<Day>();
            string qry = $"select * from {DayTableName}";
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

        public override int DeleteRange(DateTime start, DateTime end)
        {
            logger.Info($"Delete range, Start: {start} | End: {end}.........");
            Debug.WriteLine($"DeleteRange: {start}-{end}");
            int result = 0;
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                string stringstart = DateSqLite(start);
                string stringend = DateSqLite(end);
                
                cmd.CommandText = $"DELETE FROM {DayTableName} WHERE Date >= @start AND Date <= @end";
                cmd.Parameters.Add(new MySqlParameter("start", stringstart));
                cmd.Parameters.Add(new MySqlParameter("end", stringend));
                result += cmd.ExecuteNonQuery();
                
                cmd.Parameters.Clear();
                
                cmd.CommandText = $"DELETE FROM {TimeTableName} WHERE Date >= @start AND Date <= @end";
                cmd.Parameters.Add(new MySqlParameter("start", stringstart));
                cmd.Parameters.Add(new MySqlParameter("end", stringend));
                result += cmd.ExecuteNonQuery();
            }

            return result;
        }

        public override void PunchIn()
        {
            logger.Info("Punchin");
            Debug.WriteLine("PunchIn");
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS `{TimeTableName}` ( `Date` TEXT, `TimeIn` TEXT, `TimeOut` TEXT, `Key` INT )";
                cmd.ExecuteNonQuery();
                
                DateTime now = DateTime.Now;
                cmd.CommandText = $"INSERT INTO `{TimeTableName}` VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(now.Date)));
                cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(now)));
                cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(now)));
                cmd.Parameters.Add(new MySqlParameter("Key", MaxTimeId() + 1));
                cmd.ExecuteNonQuery();
            }

        }

        public override void PunchOut()
        {
            logger.Info("PunchOut");
            Debug.WriteLine("PunchOut");
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"UPDATE `{TimeTableName}` SET `TimeOut` = @TimeOut WHERE `Key` = '{MaxTimeId()}'";
                cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(DateTime.Now)));
                cmd.ExecuteNonQuery();
            }

        }

        public override List<Time> AllTimes()
        {
            List<Time> times = new List<Time>();
            string qry = $"select * from {TimeTableName}";
            DataTable vt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(qry, _connection);
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

        public override Day CurrentDay()
        {
            Day day = new Day(DateTime.MinValue);
            Debug.WriteLine("CurrentDay:");
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"select * from {DayTableName} Where Date = '{DateSqLite(DateTime.Now)}'";
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
            
            Debug.Write(day.ToString());
            return day;

        }

        public override int DeleteTime(double key)
        {
            logger.Info($"Delete time: {key}.........");
            Debug.WriteLine($"DeleteTime: {key}");
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {TimeTableName} WHERE `Key` = @Key";
                cmd.Parameters.Add(new MySqlParameter("Key", key));
                return cmd.ExecuteNonQuery();
            }
            logger.Info($"Delete time: {key}.........FINISHED!!!");

        }

        public override int UpdateDetails(DateTime date, string details)
        {
            logger.Info($"Update details, Date: {date} | Details: {details}.........");
            int result = 0;
            Debug.WriteLine($"UdateDetails: {date}:{details}");
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"UPDATE {DayTableName} SET Details = @Details WHERE Date = '{DateSqLite(date.Date)}'";
                cmd.Parameters.Add(new MySqlParameter("Details", details));
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
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"select * from {DayTableName} Where Date = @Date";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(upd.TimeIn.Date)));
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read())
                    {
                        rdr.Close();
                        AddDay(new Day(upd.TimeIn.Date));        
                    }
                }
            }

            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $"UPDATE {TimeTableName} SET `Date` = @Date, `TimeIn` = @TimeIn, `TimeOut` = @TimeOut WHERE `Key` = @Key";
                cmd.Parameters.Add(new MySqlParameter("Date", DateSqLite(upd.TimeIn.Date)));
                cmd.Parameters.Add(new MySqlParameter("TimeIn", DateTimeSqLite(upd.TimeIn)));
                cmd.Parameters.Add(new MySqlParameter("TimeOut", DateTimeSqLite(upd.TimeOut)));
                cmd.Parameters.Add(new MySqlParameter("Key", key));
                result = cmd.ExecuteNonQuery();
            }
            
            logger.Info($"Update time, Key: {key} | Update: {upd}...........FINISHED!!! Result: {result}");
            return result;

        }

        public override double MaxTimeId(string tablename = "")
        {
            if (tablename == "")
                tablename = TimeTableName;
            using (MySqlCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = $@"SELECT MAX(`Key`) FROM {tablename}";
                object result = cmd.ExecuteScalar();
                if (result.ToString() == "" || result is DBNull)
                    result = 0;
                
                return Math.Max(double.Parse(result.ToString()), 0);
            }   

        }

        public override List<Time> TimesinRange(DateTime dateA, DateTime dateB)
        {
            logger.Info($"TimesInRange, DateA: {dateA} | DateB: {dateB}.........");
            Debug.WriteLine($"TimesInRange: {dateA}-{dateB}");
            List<Time> times = new List<Time>();
            string qry = $"select * from {TimeTableName}";
            DataTable vt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(qry, _connection);
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

        public override DateTime MinDate()
        {
            using (MySqlCommand cmd = _connection.CreateCommand())
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
            using (MySqlCommand cmd = _connection.CreateCommand())
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
            throw new NotImplementedException();
        }

        public override void LoadFromServer()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            logger.Info("Disposing......");
            _connection.Close();
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

            _connection = new MySqlConnection(_connectionString);
            _connection.Open();                
        }
    }
}