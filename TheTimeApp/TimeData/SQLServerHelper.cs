﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;

namespace TheTimeApp.TimeData
{
    public class SqlServerHelper
    {
        private static readonly object SqlServerLock = new object();

        public delegate void ProgressChangedDel(float value);

        public delegate void ConnectionChangedDel(bool value);

        public delegate void ProgressFinishDel();

        public delegate void TimeDateUpdated();

        public TimeDateUpdated TimeDateaUpdate;
        public ProgressChangedDel ProgressChangedEvent;
        public ProgressFinishDel ProgressFinishEvent;
        public ConnectionChangedDel ConnectionChangedEvent;
        public ConnectionChangedDel UpdateChangedEvent;
        private List<Day> Days { get; set; }
        private List<SqlCommand> _commands;
        private bool _wasConnected;
        private readonly Timer _connectionRetry = new Timer(1000);
        private Timer _cylcicSQLRead = new Timer(5000);
        
        // your data table
        private DataTable dataTable = new DataTable();

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

        private SqlConnectionStringBuilder ConnectionStringBuilder =>
            new SqlConnectionStringBuilder() { 
                DataSource = AppSettings.SQLDataSource,
                UserID = AppSettings.SQLUserId,
                Password = AppSettings.SQLPassword,
                InitialCatalog = AppSettings.SQLCatelog,
                MultipleActiveResultSets = true,
            };

        public SqlServerHelper(List<SqlCommand> sqlCommands)
        {
            _commands = sqlCommands;
            _connectionRetry.Elapsed += OnConnectionRetry;
            _connectionRetry.Enabled = true;
            if (AppSettings.MainPermission == "write")
            {
                new Thread(() =>
                {
                    TestConnection();
                    FlushCommands();
                }).Start();    
            }

            // only start sql cyclic read if sql enabled and MainPermission is 'read'
            if ( AppSettings.SQLEnabled == "true" && AppSettings.MainPermission == "read")
            {
                _cylcicSQLRead.Elapsed += PullDataElapsed;
                _cylcicSQLRead.AutoReset = false;
                StartCyclicSqlRead();
            }
        }

        #region Helper functions

        private static List<DateTime> DatesInWeek(DateTime date)
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

        public void StartCyclicSqlRead()
        {
            _cylcicSQLRead.Start();
        }

        public void StopCyclicSqlRead()
        {
            _cylcicSQLRead.Stop();
        }
        

        private void OnConnectionRetry(object sender, ElapsedEventArgs e)
        {
            _connectionRetry.Stop();
            TestConnection();
            _connectionRetry.Start();
        }

        private static bool PingHost()
        {
            try
            {
                AppSettings.Validate();
                if (AppSettings.SQLPortNumber == "") return false;
                using (new TcpClient(AppSettings.SQLDataSource, Convert.ToInt32(AppSettings.SQLPortNumber)) {SendTimeout = 1000}) return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if successful
        /// </summary>
        /// <returns></returns>
        private void TestConnection()
        {
            bool connected = PingHost();
            if (_wasConnected != connected)
            {
                _wasConnected = connected;
                OnConnectionChanged(connected);
            }
        }

        private void OnConnectionChanged(bool connected)
        {
            ConnectionChangedEvent?.Invoke(connected);
        }
        
        #endregion

        #region SQL message pump

        private void CreateCommand(string sqlstring)
        {
            AddCommand(new SqlCommand(sqlstring));
        }

        /// <summary>
        /// Add command to list and trys to flush commands
        /// </summary>
        /// <param name="command"></param>
        private void AddCommand(SqlCommand command)
        {
            if (AppSettings.SQLEnabled == "false") return;
            new Thread(() =>
            {
                lock (SqlServerLock)
                {
                    try
                    {
                        UpdateChangedEvent?.Invoke(false);
                        _commands.Add(command);
                        FlushCommands();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                        throw;
                    }
                }       
            }).Start();
        }

        private void FlushCommands()
        {
            if (AppSettings.SQLEnabled != "true") return;
            lock (SqlServerLock)
            {
                var successful = new List<SqlCommand>();
                using (SqlConnection connection = new SqlConnection(ConnectionStringBuilder.ConnectionString))
                {
                    try
                    {
                        connection.Open();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        Console.WriteLine(e);
                    }
                    
                    foreach (SqlCommand c in _commands)
                    {
                        try
                        {
                            UpdateChangedEvent?.Invoke(false);
                            SqlCommand sqlCommand = c;
                            sqlCommand.Connection = connection;
                            int value = sqlCommand.ExecuteNonQuery();
                            Debug.WriteLine($"Excecute: {value}");
                            successful.Add(c);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }
                }

                // there is no exceptions
                foreach (SqlCommand success in successful)
                {
                    _commands.Remove(success);
                }

                TimeData.Commands = _commands;
                UpdateChangedEvent?.Invoke(_commands.Count == 0);
            }
        }

        private void FlushCommandsAsync()
        {
            new Thread(() => { FlushCommands(); }).Start();
        }

        #endregion

        #region Message generators
        
        private bool ServerContainsDay(Day day)
        {
            try
            {
                lock (SqlServerLock)
                {
                    using (SqlConnection con = new SqlConnection(ConnectionStringBuilder.ConnectionString))
                    {
                        con.Open();
                        using (SqlCommand command = new SqlCommand($@"SELECT * FROM Time_Server WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new DateTime().TimeOfDay + "')", con))
                        {
                            return command.ExecuteScalar() != null;
                        }                            
                    }
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
                                                                TimeOut = @TimeOut AND CONVERT(VARCHAR,Details) = @Details)"))// todo, must add connection using statemnt
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

        public void PullFromServer(List<Day> days)
        {
            lock (SqlServerLock)
            {
                Days = days;

                // removes deleted from server
                SqlCommand dayCommand = new SqlCommand("SELECT * FROM Time_Server WHERE (TimeIn = '" + new TimeSpan() + "' AND TimeOut = '" + new TimeSpan() + "')");// todo, must add connection using statement
                SqlDataReader dayReader = dayCommand.ExecuteReader();
                while (dayReader.Read())
                {
                    if (dayReader["TimeIn"] is TimeSpan && dayReader["TimeOut"] is TimeSpan)
                    {
                        if ((TimeSpan) dayReader["TimeIn"] == new TimeSpan() && (TimeSpan) dayReader["TimeOut"] == new TimeSpan()) // day header
                        {
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

                SqlCommand timeCommand = new SqlCommand("SELECT * FROM Time_Server WHERE (TimeIn <> '" + new TimeSpan() + "' AND TimeOut <> '" + new TimeSpan() + "')");// todo must add connection using statement
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
                            using (SqlCommand command = new SqlCommand("DELETE FROM Time_Server WHERE( TimeIn = '" + (TimeSpan) timeReader["TimeIn"] + "' AND TimeOut = '" + (TimeSpan) timeReader["TimeOut"] + "')"))
                            {
                                AddCommand(command);
                            }
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
                lock (SqlServerLock)
                {
                    CreateCommand(@"IF EXISTS (SELECT * FROM sysobjects WHERE name='Time_Server' AND xtype='U') DROP TABLE Time_Server");
                    CreateCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Time_Server' AND xtype='U')
                                            CREATE TABLE Time_Server (Date date, TimeIn time, TimeOut time, Details text)");
                    ProgressChangedEvent?.Invoke(0);
                    for (float i = 0; i < days.Count; i++)
                    {
                        Day day = days[(int) i];
                        InsertDay(day);
                        foreach (var time in day.GetTimes())
                        {
                            InsertTime(time);
                        }

                        ProgressChangedEvent?.Invoke(i / days.Count * 100f);
                    }

                    ProgressFinishEvent?.Invoke();
                }
            }).Start();
        }

        public void RemoveWeek(DateTime date)
        {
            Debug.WriteLine("Remove week");
            var datesInWeek = DatesInWeek(date);
            AddCommand(new SqlCommand(@"DELETE FROM Time_Server WHERE( Date = '" + datesInWeek[0] + "' OR Date = '" + datesInWeek[1] + "' OR Date = '" + datesInWeek[2] + "' OR Date = '" + datesInWeek[3] + "' OR Date = '" + datesInWeek[4] +
                                      "' OR Date = '" + datesInWeek[5] + "' OR Date = '" + datesInWeek[6] + "')"));
        }
        
        /// <summary>
        /// Inserts day into data base
        /// </summary>
        /// <param name="day"></param>
        public void InsertDay(Day day)
        {
            Debug.WriteLine("Insert day");
            if (day == null) return;
            try
            {
                using (_connectionRetry)
                {
                    
                }
                using (SqlCommand command = new SqlCommand("INSERT INTO Time_Server VALUES(@Date, @TimeIn, @TimeOut, @Details)"))
                {
                    command.Parameters.Add(new SqlParameter("Date", day.Date));
                    command.Parameters.Add(new SqlParameter("TimeIn", new DateTime().TimeOfDay));
                    command.Parameters.Add(new SqlParameter("TimeOut", new DateTime().TimeOfDay));
                    command.Parameters.Add(new SqlParameter("Details", day.Details));
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
        /// Removes all times in this day as well!
        /// </summary>
        /// <param name="date"></param>
        public void RemoveDay(DateTime date)
        {
            Debug.WriteLine("Remove day");
            AddCommand(new SqlCommand(@"DELETE FROM Time_Server WHERE( Date = '" + date + "')"));
        }

        /// <summary>
        /// Just removes the day row.
        /// </summary>
        /// <param name="day"></param>
        public void RemoveDayHeader(Day day)
        {
            Debug.WriteLine("Remove day header");
            AddCommand(new SqlCommand("DELETE FROM Time_Server WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new TimeSpan() + "')"));
        }
        
        public void InsertTime(Time time)
        {
            Debug.WriteLine("Insert time");
            if (time == null) return;
            try
            {
                using (SqlCommand command = new SqlCommand("INSERT INTO Time_Server VALUES(@Date, @TimeIn, @TimeOut, @Details) "))
                {
                    command.Parameters.Add(new SqlParameter("Date", time.TimeIn.Date));
                    command.Parameters.Add(new SqlParameter("TimeIn", time.TimeIn.TimeOfDay));
                    command.Parameters.Add(new SqlParameter("TimeOut", time.TimeOut.TimeOfDay));
                    command.Parameters.Add(new SqlParameter("Details", ""));
                    AddCommand(command);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        public void RemoveTime(Time time)
        {
            Debug.WriteLine("Remove time");
            AddCommand(new SqlCommand(@"DELETE FROM Time_Server WHERE( Date = '"+ time.TimeIn.Date +"' TimeIn = '" + time.TimeIn.TimeOfDay + "' AND TimeOut = '" + time.TimeOut.TimeOfDay + "')"));
        }

        public void UpdateDetails(Day day)
        {
            Debug.WriteLine("Update details");
            if (day.Details == "" && day.Times.Count == 0)// day is removed in local data if no time and details == ""
            {
                RemoveDayHeader(day);
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand("UPDATE Time_Server SET Details = @Details WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new TimeSpan() + "')"))
                {
                    cmd.Parameters.AddWithValue("@Details", day.Details);
                    AddCommand(cmd);
                }                    
            }
        }
        
        public void UpdateTime(Time time)
        {
            Debug.WriteLine("Update time");
            using (SqlCommand cmd = new SqlCommand("UPDATE Time_Server SET TimeIn = @TimeIn, TimeOut = @TimeOut WHERE( Date = '" + time.TimeIn.Date + "' AND TimeIn = '" + time.TimeIn.TimeOfDay + "' OR TimeOut = '" + time.TimeOut.TimeOfDay + "')"))
            {
                cmd.Parameters.AddWithValue("@TimeIn", time.TimeIn.TimeOfDay);
                cmd.Parameters.AddWithValue("@TimeOut", time.TimeOut.TimeOfDay);
                AddCommand(cmd);
            }   
        }
        
        #endregion

        public void LoadDataFromServer(string savetoFile)// todo, not finished!
        {
            lock (SqlServerLock)
            {
                try
                {
                    string query = "SELECT * FROM Time_Server";

                    using (SqlConnection conn = new SqlConnection(ConnectionStringBuilder.ConnectionString))
                    {
                        conn.Open();

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                TimeData data = new TimeData();
                                DataTable temp = new DataTable();
                                da.Fill(temp);

                                for (int i = 0; i < temp.Rows.Count; i++)
                                {
                                    DataRow row = temp.Rows[i];

                                    if (row.ItemArray[1] is TimeSpan)
                                    {
                                        if (((TimeSpan) row.ItemArray[1]) == new TimeSpan()) // if this is a day header
                                        {
                                            if (row.ItemArray[0] is DateTime)
                                            {
                                                if (!data.Days.Exists(d => d.Date == (DateTime) row.ItemArray[0]))
                                                {
                                                    data.Days.Add(new Day((DateTime) row.ItemArray[0]) {Details = row.ItemArray[3].ToString()});
                                                }
                                            }
                                        }
                                    }

                                    ProgressChangedEvent?.Invoke(100f / temp.Rows.Count / 2 * i);
                                }

                                for (int i = 0; i < data.Days.Count; i++)
                                {
                                    Day day = data.Days[i];

                                    foreach (DataRow row in temp.Rows)
                                    {
                                        if (row.ItemArray[0] is DateTime && row.ItemArray[1] is TimeSpan && row.ItemArray[2] is TimeSpan)
                                        {
                                            if ((DateTime) row.ItemArray[0] == day.Date)
                                            {
                                                if (((TimeSpan) row.ItemArray[1]) != new TimeSpan()) // if this is not a day header
                                                {
                                                    TimeSpan intime = (TimeSpan) row.ItemArray[1];
                                                    TimeSpan outtime = (TimeSpan) row.ItemArray[2];
                                                    day.Times.Add(new Time(new DateTime(day.Date.Year, day.Date.Month, day.Date.Day, intime.Hours, intime.Minutes, intime.Seconds),
                                                        new DateTime(day.Date.Year, day.Date.Month, day.Date.Day, outtime.Hours, outtime.Minutes, outtime.Seconds)));
                                                }
                                            }
                                        }
                                    }
                                    ProgressChangedEvent?.Invoke(100f / temp.Rows.Count / 2 * i + 50);
                                }
                                data.Save(savetoFile);
                                ProgressChangedEvent?.Invoke(100);
                            }
                        }
                        TimeDateaUpdate?.Invoke();// Fire event for updating ui
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    Console.WriteLine(e);
                }
            }
        }

        #region pull data cyclic read

        private void PullDataElapsed(object sender, ElapsedEventArgs e)
        {
            PullData();
            StartCyclicSqlRead();
        }
        
        // your method to pull data from database to datatable   
        private void PullData()
        {
            try
            {
                string query = "SELECT * FROM Time_Server";

                    using (SqlConnection conn = new SqlConnection(ConnectionStringBuilder.ConnectionString))
                    {
                        conn.Open();
                    
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable temp = new DataTable();
                                da.Fill(temp);

                                if (dataTable.Columns.Count != 0 && !AreDifferent(temp, dataTable))
                                {
                                    return;
                                }

                                dataTable = temp;

                            LoadDataFromServer(AppSettings.DataPath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                Console.WriteLine(e);
            }
        }
        
        private static bool AreDifferent(DataTable FirstDataTable, DataTable SecondDataTable)
        {
            //Create Empty Table   
            DataTable ResultDataTable = new DataTable("ResultDataTable");

            //use a Dataset to make use of a DataRelation object   
            using (DataSet ds = new DataSet())
            {
                //Add tables   
                ds.Tables.AddRange(new DataTable[] {FirstDataTable.Copy(), SecondDataTable.Copy()});

                //Get Columns for DataRelation   
                DataColumn[] firstColumns = new DataColumn[ds.Tables[0].Columns.Count];
                for (int i = 0; i < firstColumns.Length; i++)
                {
                    firstColumns[i] = ds.Tables[0].Columns[i];
                }

                DataColumn[] secondColumns = new DataColumn[ds.Tables[1].Columns.Count];
                for (int i = 0; i < secondColumns.Length; i++)
                {
                    secondColumns[i] = ds.Tables[1].Columns[i];
                }

                //Create DataRelation   
                DataRelation r1 = new DataRelation(string.Empty, firstColumns, secondColumns, false);
                ds.Relations.Add(r1);

                DataRelation r2 = new DataRelation(string.Empty, secondColumns, firstColumns, false);
                ds.Relations.Add(r2);

                //Create columns for return table   
                for (int i = 0; i < FirstDataTable.Columns.Count; i++)
                {
                    ResultDataTable.Columns.Add(FirstDataTable.Columns[i].ColumnName, FirstDataTable.Columns[i].DataType);
                }

                //If FirstDataTable Row not in SecondDataTable, Add to ResultDataTable.   
                ResultDataTable.BeginLoadData();
                foreach (DataRow parentrow in ds.Tables[0].Rows)
                {
                    DataRow[] childrows = parentrow.GetChildRows(r1);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }

                //If SecondDataTable Row not in FirstDataTable, Add to ResultDataTable.   
                foreach (DataRow parentrow in ds.Tables[1].Rows)
                {
                    DataRow[] childrows = parentrow.GetChildRows(r2);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }

                ResultDataTable.EndLoadData();
            }

            return ResultDataTable.Rows.Count > 0;
        }  


        #endregion

    }
}