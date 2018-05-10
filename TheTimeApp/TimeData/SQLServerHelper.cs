using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Ink;

namespace TheTimeApp.TimeData
{
    public class SQLServerHelper
    {
        private static object _lock = new Object();
        private static bool _kill;

        private static List<Day> Days { get; set; }

        private static List<Time> Times
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
        
        private static SqlConnectionStringBuilder cb = new SqlConnectionStringBuilder()
        {
            DataSource = AppSettings.SQLDataSource,
            UserID = AppSettings.SQLUserId,
            Password = AppSettings.SQLPassword,
            InitialCatalog = AppSettings.SQLCatelog,
            MultipleActiveResultSets = true
        };
        
        public static void PushToServer(List<Day> days)
        {
            new Thread(() =>
            {
                lock (_lock)
                {
                    Days = days;
                    
                    CreateCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Time_Server' AND xtype='U')
                                            CREATE TABLE Time_Server (Date date, TimeIn time, TimeOut time, Details text)");

                    using (SqlConnection con = new SqlConnection(cb.ConnectionString))
                    {
                        con.Open();
                        
                        // removes deleted from server
                        SqlCommand myCommand = new SqlCommand("SELECT * FROM Time_Server",  con);
                        SqlDataReader myReader = myCommand.ExecuteReader();
                        
                        while(myReader.Read())
                        {
                            if (_kill) 
                                return;

                            if (myReader["TimeIn"] is TimeSpan && myReader["TimeOut"] is TimeSpan)
                            {
                                if ((TimeSpan) myReader["TimeIn"] == new TimeSpan() && (TimeSpan) myReader["TimeOut"] == new TimeSpan())// day header
                                {
                                    Day day = new Day((DateTime) myReader["Date"]) {Details = myReader["Details"].ToString()};
                                    bool contains = false;
                                    foreach (Day d in Days)
                                    {
                                        if (d.Date == (DateTime)myReader["Date"] && (TimeSpan)myReader["TimeIn"] == new TimeSpan())
                                        {
                                            contains = true;
                                            break;
                                        }
                                    }

                                    if (!contains)
                                    {
                                        RemoveDayHeader((DateTime)myReader["Date"],con);
                                    }
                                }
                                else// time entry
                                {
                                    bool contains = false;
                                    foreach (Time t in Times)
                                    {
                                        if(t.TimeIn.TimeOfDay == (TimeSpan) myReader["TimeIn"] && t.TimeOut.TimeOfDay == (TimeSpan) myReader["TimeOut"] )
                                        {
                                            contains = true;
                                            break;
                                        }
                                    }

                                    if (!contains)
                                    {
                                        RemoveTime((TimeSpan) myReader["TimeIn"], (TimeSpan) myReader["TimeOut"],con);
                                    }
                                }
                            }
                        }
                        
                        foreach (Day day in days)
                        {
                            if (_kill) 
                                return;
                            
                            InsertDayHeader(day, con);
                            foreach (var time in day.GetTimes())
                            {
                                if (_kill) 
                                    return;
                                InsertTime(time, con);
                            }
                        }
                        
                    }
                }
            }).Start();
        }
        
        private static void CreateCommand(string queryString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(cb.ConnectionString))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                }
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
        private static void RemoveDayHeader(DateTime date, SqlConnection con)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand("DELETE FROM Time_Server WHERE( Date = '" + date + "' AND TimeIn = '" + new TimeSpan() + "')", con))
                {
                    cmd.ExecuteNonQuery();
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
        private static void RemoveTime(TimeSpan timein, TimeSpan timeout, SqlConnection con)
        {
            try
            {
                using (SqlCommand command = new SqlCommand("DELETE FROM Time_Server WHERE( TimeIn = '" + timein + "' AND TimeOut = '" + timeout + "')", con))
                {
                    command.ExecuteNonQuery();
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
        private static void InsertDayHeader(Day day, SqlConnection con = null)
        {
            if (day == null) return;
            
            SqlConnection connection = con == null ? new SqlConnection(cb.ConnectionString) : con;
            
            try
            {
                if (ServerContainsDay(day, con))
                {
                    using (SqlCommand cmd = new SqlCommand("UPDATE Time_Server SET Details = @Details WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new DateTime().TimeOfDay + "')", con))
                    {
                        cmd.Parameters.AddWithValue("@Details", day.Details);
                        int rows = cmd.ExecuteNonQuery();
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
                        command.ExecuteNonQuery();
                    }    
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        private static void InsertTime(Time time, SqlConnection con)
        {
            if (time == null) return;
            
            try
            {
                if (ServerContainsTime(time, con))
                {
                    using (SqlCommand command = new SqlCommand($@"UPDATE Time_Server SET Date = @Date, TimeIn = @TimeIn, TimeOut = @TimeOut, Details = @Details 
                         WHERE Date = '{time.TimeIn.Date}' AND TimeIn = '{time.TimeIn.TimeOfDay}' OR TimeOut = '{time.TimeOut.TimeOfDay}' ", con))
                    {
                        command.Parameters.Add(new SqlParameter("Date", time.TimeIn.Date));
                        command.Parameters.Add(new SqlParameter("TimeIn", time.TimeIn.TimeOfDay));
                        command.Parameters.Add(new SqlParameter("TimeOut", time.TimeOut.TimeOfDay));
                        command.Parameters.Add(new SqlParameter("Details", ""));
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (SqlCommand command = new SqlCommand("INSERT INTO Time_Server VALUES(@Date, @TimeIn, @TimeOut, @Details) ", con))
                    {
                        command.Parameters.Add(new SqlParameter("Date", time.TimeIn.Date));
                        command.Parameters.Add(new SqlParameter("TimeIn", time.TimeIn.TimeOfDay));
                        command.Parameters.Add(new SqlParameter("TimeOut", time.TimeOut.TimeOfDay));
                        command.Parameters.Add(new SqlParameter("Details", ""));
                        command.ExecuteNonQuery();
                    }
                    
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        private static bool ServerContainsDay(Day day, SqlConnection con)
        {
            try
            {
                using (SqlCommand command = new SqlCommand($@"SELECT * FROM Time_Server WHERE( Date = '" + day.Date + "' AND TimeIn = '" 
                                                           + new DateTime().TimeOfDay + "' )", con))
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
        
        private static bool ServerContainsTime(Time time, SqlConnection con)
        {
            try
            {
                using (SqlCommand command = new SqlCommand(@"SELECT * FROM Time_Server WHERE( Date = @Date AND TimeIn = @TimeIn AND 
                                                                TimeOut = @TimeOut AND CONVERT(VARCHAR,Details) = @Details)", con))
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
    }
}