using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using MessageBox = System.Windows.MessageBox;

namespace TheTimeApp.TimeData
{
    public class SQLServerHelper : IDisposable
    {
        private static object _lock = new Object();

        public static bool IsRePushingSQL { get; private set; }
    
        public delegate void ProgressChangedDel(float value);
        
        public delegate void ProgressFinishDel();
        
        public ProgressChangedDel ProgressChangedEvent;

        public ProgressFinishDel ProgressFinishEvent;

        private List<Day> Days { get; set; }
        
        private List<SqlCommand> _commands = new List<SqlCommand>();

        private SqlConnection connection;

        private List<Time> Times
        {
            get{
                var times = new List<Time>();
                foreach (Day day in Days)
                {
                    foreach (Time dayTime in day.Times)
                    {
                        times.Add(dayTime);
                    }
                }

                return times;
            }
        }
        
        private SqlConnectionStringBuilder cb = new SqlConnectionStringBuilder()
        {
            DataSource = AppSettings.SQLDataSource,
            UserID = AppSettings.SQLUserId,
            Password = AppSettings.SQLPassword,
            InitialCatalog = AppSettings.SQLCatelog,
            MultipleActiveResultSets = true
        };

        public SQLServerHelper()
        {
            connection = new SqlConnection(cb.ConnectionString);
            connection.Open();
        }

        public void PullFromServer(List<Day> days)
        {
            lock (_lock)
            {
                Days = days;

                // removes deleted from server
                SqlCommand dayCommand = new SqlCommand("SELECT * FROM Time_Server WHERE (TimeIn = '" + new TimeSpan() + "' AND TimeOut = '" + new TimeSpan() + "')", connection);
                SqlDataReader dayReader = dayCommand.ExecuteReader();

                while (dayReader.Read())
                {
                    if (dayReader["TimeIn"] is TimeSpan && dayReader["TimeOut"] is TimeSpan)
                    {
                        if ((TimeSpan) dayReader["TimeIn"] == new TimeSpan() && (TimeSpan) dayReader["TimeOut"] == new TimeSpan()) // day header
                        {
                            Day day = new Day((DateTime) dayReader["Date"]) {Details = dayReader["Details"].ToString()};
                            bool contains = false;
                            foreach (Day d in Days)
                            {
                                if (d.Date == (DateTime) dayReader["Date"] && (TimeSpan) dayReader["TimeIn"] == new TimeSpan())
                                {
                                    contains = true;
                                    break;
                                }
                            }

                            if (!contains)
                            {
                                days.Add(new Day((DateTime) dayReader["Date"]));
                            }
                        }
                    }
                }

                SqlCommand timeCommand = new SqlCommand("SELECT * FROM Time_Server WHERE (TimeIn <> '" + new TimeSpan() + "' AND TimeOut <> '" + new TimeSpan() + "')", connection);
                SqlDataReader timeReader = timeCommand.ExecuteReader();

                while (timeReader.Read())
                {
                    if (timeReader["TimeIn"] is TimeSpan && timeReader["TimeOut"] is TimeSpan)
                    {
                        bool contains = false;
                        foreach (Time t in Times)
                        {
                            if (t.TimeIn.TimeOfDay == (TimeSpan) timeReader["TimeIn"] && t.TimeOut.TimeOfDay == (TimeSpan) timeReader["TimeOut"])
                            {
                                contains = true;
                                break;
                            }
                        }

                        if (!contains)
                        {
                            RemoveTime((TimeSpan) timeReader["TimeIn"], (TimeSpan) timeReader["TimeOut"], connection);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Delete sql table and repushes everything
        /// </summary>
        /// <param name="days"></param>
        public void RePushToServer(List<Day> days)
        {
            new Thread(() =>
            {
                lock (_lock)
                {
                    IsRePushingSQL = true;
                    CreateCommand(@"IF EXISTS (SELECT * FROM sysobjects WHERE name='Time_Server' AND xtype='U') DROP TABLE Time_Server");
                    CreateCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Time_Server' AND xtype='U')
                                            CREATE TABLE Time_Server (Date date, TimeIn time, TimeOut time, Details text)");

                    ProgressChangedEvent?.Invoke(0);
                    for (float i = 0; i < days.Count; i++)
                    {
                        Day day = days[(int) i];
                        InsertOrUpdateDayHeader(day);
                        foreach (var time in day.GetTimes())
                        {
                            InsertTime(time);
                        }

                        ProgressChangedEvent?.Invoke(i / days.Count * 100f);
                    }

                    ProgressFinishEvent?.Invoke();
                    IsRePushingSQL = false;
                }
            }).Start();
        }

        private void CreateCommand(string sqlstring)
        {
            AddCommand(new SqlCommand(sqlstring,connection));
        }

        /// <summary>
        /// Add command to list and trys to flush commands
        /// </summary>
        /// <param name="command"></param>
        private void AddCommand(SqlCommand command)
        {
            try
            {
                _commands.Add(command);
                FlushCommands();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Removes day from data base
        /// </summary>
        /// <param name="day"></param>
        /// <param name="con"></param>
        private void RemoveDayHeader(DateTime date, SqlConnection con)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand("DELETE FROM Time_Server WHERE( Date = '" + date + "' AND TimeIn = '" + new TimeSpan() + "')", con))
                {
                    AddCommand(cmd);
                    //rows number of record got updated
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }
        
        /// <summary>
        /// Removes time are from sql database
        /// </summary>
        /// <param name="time"></param>
        /// <param name="con"></param>
        private void RemoveTime(TimeSpan timein, TimeSpan timeout, SqlConnection con)
        {
            try
            {
                using (SqlCommand command = new SqlCommand("DELETE FROM Time_Server WHERE( TimeIn = '" + timein + "' AND TimeOut = '" + timeout + "')", con))
                {
                    AddCommand(command);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Inserts day into data base
        /// </summary>
        /// <param name="day"></param>
        /// <param name="con"></param>
        private void InsertOrUpdateDayHeader(Day day)
        {
            if (day == null) return;

            lock (_lock)
            {
                try
                {
                    if (ServerContainsDay(day))
                    {
                        using (SqlCommand cmd = new SqlCommand("UPDATE Time_Server SET Details = @Details WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new DateTime().TimeOfDay + "')", connection))
                        {
                            cmd.Parameters.AddWithValue("@Details", day.Details);
                            AddCommand(cmd);
                            //rows number of record got updated
                        }
                    }
                    else
                    {
                        using (SqlCommand command = new SqlCommand("INSERT INTO Time_Server VALUES(@Date, @TimeIn, @TimeOut, @Details)", connection))
                        {
                            command.Parameters.Add(new SqlParameter("Date", day.Date));
                            command.Parameters.Add(new SqlParameter("TimeIn", new DateTime().TimeOfDay));
                            command.Parameters.Add(new SqlParameter("TimeOut", new DateTime().TimeOfDay));
                            command.Parameters.Add(new SqlParameter("Details", day.Details));
                            AddCommand(command);
                        }    
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    throw;
                }                
            }
        }

        public void InsertTime(Time time)
        {
            if (time == null) return;
            
            try
            {
                if (ServerContainsTime(time))
                {
                    using (SqlCommand command = new SqlCommand($@"UPDATE Time_Server SET Date = @Date, TimeIn = @TimeIn, TimeOut = @TimeOut, Details = @Details 
                         WHERE Date = '{time.TimeIn.Date}' AND TimeIn = '{time.TimeIn.TimeOfDay}' OR TimeOut = '{time.TimeOut.TimeOfDay}' ", connection))
                    {
                        command.Parameters.Add(new SqlParameter("Date", time.TimeIn.Date));
                        command.Parameters.Add(new SqlParameter("TimeIn", time.TimeIn.TimeOfDay));
                        command.Parameters.Add(new SqlParameter("TimeOut", time.TimeOut.TimeOfDay));
                        command.Parameters.Add(new SqlParameter("Details", ""));
                        AddCommand(command);
                    }
                }
                else
                {
                    using (SqlCommand command = new SqlCommand("INSERT INTO Time_Server VALUES(@Date, @TimeIn, @TimeOut, @Details) ", connection))
                    {
                        command.Parameters.Add(new SqlParameter("Date", time.TimeIn.Date));
                        command.Parameters.Add(new SqlParameter("TimeIn", time.TimeIn.TimeOfDay));
                        command.Parameters.Add(new SqlParameter("TimeOut", time.TimeOut.TimeOfDay));
                        command.Parameters.Add(new SqlParameter("Details", ""));
                        AddCommand(command);
                    }
                    
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        private void FlushCommands()
        {
            new Thread(() =>
            {
                lock (_lock)
                {
                    var successful = new List<SqlCommand>();
                    try
                    {
                        foreach (SqlCommand sqlCommand in _commands)
                        {
                            sqlCommand.Connection = connection;
                            sqlCommand.ExecuteNonQuery();
                            successful.Add(sqlCommand);
                        }

                        foreach (SqlCommand success in successful)
                        {
                            _commands.Remove(success);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                    }    
                }    
            }).Start();
        }

        private bool ServerContainsDay(Day day)
        {
            try
            {
                using (SqlCommand command = new SqlCommand($@"SELECT * FROM Time_Server WHERE( Date = '" + day.Date + "' AND TimeIn = '" 
                                                           + new DateTime().TimeOfDay + "' )", connection))
                {
                    return command.ExecuteScalar() != null;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }
        
        private bool ServerContainsTime(Time time)
        {
            try
            {
                using (SqlCommand command = new SqlCommand(@"SELECT * FROM Time_Server WHERE( Date = @Date AND TimeIn = @TimeIn AND 
                                                                TimeOut = @TimeOut AND CONVERT(VARCHAR,Details) = @Details)", connection))
                {
                    command.Parameters.AddWithValue("@Date", time.TimeIn.Date);
                    command.Parameters.AddWithValue("@TimeIn", time.TimeIn.TimeOfDay);
                    command.Parameters.AddWithValue("@TimeOut", time.TimeOut.TimeOfDay);
                    command.Parameters.AddWithValue("@Details", "");
                    return command.ExecuteScalar() != null;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        public void Dispose()
        {
            FlushCommands();
            connection?.Close();
            connection?.Dispose();
        }
        
        private List<DateTime> DatesAreInTheSameWeek(DateTime date)
        {
            int currentDayOfWeek = (int) date.DayOfWeek;
            DateTime sunday = date.AddDays(-currentDayOfWeek);
            DateTime monday = sunday.AddDays(1);
            // If we started on Sunday, we should actually have gone *back*
            // 6 days instead of forward 1...
            if (currentDayOfWeek == 0)
            {
                monday = monday.AddDays(-7);
            }
            return Enumerable.Range(0, 7).Select(days => monday.AddDays(days)).ToList();
        }

        public void RemoveWeek(DateTime date)
        {
            List<DateTime> datesInWeek = DatesAreInTheSameWeek(date);
            using (SqlCommand command = new SqlCommand(@"DELETE FROM Time_Server WHERE( Date = '" + datesInWeek[0] + "' OR Date = '" + datesInWeek[1] + "' OR Date = '" + datesInWeek[2] + 
                     "' OR Date = '" + datesInWeek[3] + "' OR Date = '" + datesInWeek[4] + "' OR Date = '" + datesInWeek[5] + "' OR Date = '" + datesInWeek[6] + "')", connection))
                
            {
                AddCommand(command);
            }
        }

        public void RemoveDay(DateTime date)
        {
            using (SqlCommand command = new SqlCommand(@"DELETE FROM Time_Server WHERE( Date = '" + date + "')", connection))
            {
                AddCommand(command);
            }
        }

        public void DeleteTime(Time time)
        {
            using (SqlCommand command = new SqlCommand(@"DELETE FROM Time_Server WHERE( TimeIn = '" + time.TimeIn.TimeOfDay + "' AND TimeOut = '" + time.TimeOut.TimeOfDay + "')", connection))
            {
                AddCommand(command);
            }
        }

        public void UpdateDetails(Day day)
        {
            using (SqlCommand cmd = new SqlCommand("UPDATE Time_Server SET Details = @Details WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new DateTime().TimeOfDay + "')", connection))
            {
                cmd.Parameters.AddWithValue("@Details", day.Details);
                AddCommand(cmd);
            }
        }
    }
}