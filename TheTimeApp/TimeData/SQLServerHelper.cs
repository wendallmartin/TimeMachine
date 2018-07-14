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
using DataBase = TheTimeApp.TimeData.TimeData;

namespace TheTimeApp.TimeData
{
    public class SqlServerHelper
    {
        private static readonly object IsConnectedLock = new object();
        private static readonly object SqlServerLock = new object();
        private static readonly object SqlPullLock = new object();
        private static readonly object SqlPushLock = new object();
        
        private SqlConnection _connection = new SqlConnection();// Only referenced from SqlConnection property!
        
        public List<SerilizeSqlCommand> Commands;

        #region Delegates

        public delegate void ProgressChangedDel(float value);
        
        public delegate void UpdateChangeDel(List<SerilizeSqlCommand> value);

        public delegate void ConnectionChangedDel(bool value);

        public delegate void ProgressFinishDel();

        public delegate void TimeDateUpdatedDel(List<Day> data);

        #endregion

        #region Events

        public TimeDateUpdatedDel TimeDateaUpdate;
        public ProgressChangedDel ProgressChangedEvent;
        public ProgressFinishDel ProgressFinishEvent;
        public ConnectionChangedDel ConnectionChangedEvent;
        public UpdateChangeDel UpdateChangedEvent;

        #endregion
        
        private bool _wasConnected;
        private readonly Timer _connectionRetry = new Timer(1000);
        private readonly Timer _cylcicSqlRead = new Timer(5000);

        public SqlConnectionStringBuilder ConnectionStringBuilder { get; set; }
        
        public int Port { get; set; }
        
        public SqlMode SqlMode { get; set; }
        
        // your data table
        private DataTable _dataTable = new DataTable();

        public bool SqlEnabled { get; set; } = true;

        /// <summary>
        /// If connection == null we are not
        /// connected to server.
        /// </summary>
        private SqlConnection SqlConnection
        {
            get{
                try
                {
                    if (!IsConnected) return null;
                    
                    if (_connection.State != ConnectionState.Open)
                    {
                        _connection = new SqlConnection(ConnectionStringBuilder.ConnectionString);
                        _connection.Open();
                    }
                    return _connection;
                }
                catch (Exception e)
                {
                    Logger.LogException(e, "SqlConnectionExceptions");
                    return null;
                }
            }
        }

        private string CurrentUser => ToTableName(DataBase.TimeDataBase.CurrentUserName);

        /// <summary>
        /// Returns user name converted to table name.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private string ToTableName(string username)
        {
            return username.Replace(' ', '_') + "_TimeTable";
        }

        public SqlServerHelper(SqlConnectionStringBuilder conStringBuilder, SqlMode sqlMode, List<SerilizeSqlCommand> commands)
        {
            ConnectionStringBuilder = conStringBuilder;
            SqlMode = sqlMode;
            Commands = commands;
            
            if(Commands == null)
                Commands = new List<SerilizeSqlCommand>();
            
            _connectionRetry.Elapsed += OnConnectionRetry;
            _connectionRetry.Enabled = true;
            if (SqlMode.Write && SqlEnabled)
            {
                new Thread(TestConnection).Start();
                CreateUser(TimeData.TimeDataBase.CurrentUserName);
            }

            // only start sql cyclic read if sql enabled and MainPermission is 'read'
            if (SqlEnabled && SqlMode.Read && IsConnected)
            {
                _cylcicSqlRead.Elapsed += PullDataElapsed;
                _cylcicSqlRead.AutoReset = false;
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
            _cylcicSqlRead.Start();
        }

        private void OnConnectionRetry(object sender, ElapsedEventArgs e)
        {
            _connectionRetry.Stop();
            TestConnection();
            _connectionRetry.Start();
        }

        public bool IsConnected
        {
            get{

                lock (IsConnectedLock)
                {
                    try
                    {
                        if (Port == 0) return false;
                        using (new TcpClient(ConnectionStringBuilder.DataSource, Port) {SendTimeout = 1000})
                        {
                            return true;
                        }
                    }
                    catch (SocketException)
                    {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if successful
        /// </summary>
        /// <returns></returns>
        private void TestConnection()
        {
            bool connected = IsConnected;
            if (_wasConnected != connected)
            {
                _wasConnected = connected;
                OnConnectionChanged(connected);
            }

            if (connected)
            {
                lock (SqlServerLock)
                {
                    FlushCommands();    
                }
            }
        }

        private void OnConnectionChanged(bool connected)
        {
            ConnectionChangedEvent?.Invoke(connected);
        }

        #endregion

        #region SQL message pump

        public List<string> GetAllTables()
        {
            if (!SqlEnabled || SqlConnection == null)
                return null;
            var tables = new List<string>();
            
            DataTable names = SqlConnection.GetSchema("Tables");
            foreach (DataRow dataRow in names.Rows)
            {
                string tableName = dataRow[2].ToString();
                if (tableName.Contains("_TimeTable"))
                {
                    tableName = tableName.Replace("_TimeTable", "");
                    tables.Add(tableName);
                }
            }

            return tables;
        }

        /// <summary>
        /// Creates user table if it doesn't exist
        /// EXCEPTS USER NAME NOT USER TABLE NAME!!
        /// </summary>
        /// <param name="user"></param>
        public void CreateUser(string user)
        {
            if (!SqlEnabled) return;
            AddCommand(new SerilizeSqlCommand($@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{ToTableName(user)}')
                                            CREATE TABLE {ToTableName(user)} (Date date, TimeIn time, TimeOut time, Details text)"));
        }

        /// <summary>
        /// Deletes user table if it exist
        /// EXCEPTS USER NAME NOT USER TABLE NAME!!
        /// </summary>
        /// <param name="username"></param>
        public void RemoveUser(string username)
        {
            if (!SqlEnabled) return;
            AddCommand(new SerilizeSqlCommand($@"IF EXISTS (SELECT * FROM sysobjects WHERE name='{ToTableName(username)}' AND xtype='U') DROP TABLE {ToTableName(username)}"));
        }

        /// <summary>
        /// Add command to list and trys to flush commands
        /// </summary>
        /// <param name="command"></param>
        private void AddCommand(SerilizeSqlCommand command)
        {
            if (!SqlEnabled) return;
            command.CommandAddTime = DateTime.Now;
            new Thread(() =>
            {
                lock (SqlServerLock) // Moving the lock here as keeps the SerilizableCommands list from changing while flushing. THE ONLY PLACE FLUSHCOMMANDS IS CALLED!
                {
                    Commands.Add(command);
                    UpdateChangedEvent?.Invoke(Commands);
                }
            }).Start();
        }

        private void FlushCommands()
        {
            if (!SqlEnabled || SqlConnection == null || SqlConnection.State != ConnectionState.Open)
                return;
            
            try
            {
                var successful = new List<SerilizeSqlCommand>();
                Commands = Commands.OrderBy(c => c.CommandAddTime).ToList();
                foreach (SerilizeSqlCommand serilizedCommand in Commands)
                {
                    try
                    {

                        SqlCommand command = serilizedCommand.GetSqlCommand;
                        command.Connection = SqlConnection;
                        command.ExecuteNonQuery();
                        successful.Add(serilizedCommand);
                    }
                    catch (Exception e)
                    {
                        Logger.LogException(e, "SqlExeptions");
                    }
                    UpdateChangedEvent?.Invoke(Commands.Where(s => !successful.Contains(s)).ToList());
                }

                // there is no exceptions
                foreach (SerilizeSqlCommand success in successful)
                {
                    Commands.Remove(success);
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e, "FlushExceptions");
            }
        }

        #endregion

        #region Message generators

        /// <summary>
        /// Delete sql table and repushes everything
        /// </summary>
        /// <param name="days"></param>
        public void RePushToServer(List<Day> days)
        {
            if (!SqlEnabled)
            {
                MessageBox.Show("SQL not enabled!");
                return;
            }

            new Thread(() =>
            {
                lock (SqlPushLock)
                {
                    AddCommand(new SerilizeSqlCommand($@"IF EXISTS (SELECT * FROM sysobjects WHERE name='{CurrentUser}' AND xtype='U') DROP TABLE {CurrentUser}"));
                    AddCommand(new SerilizeSqlCommand($@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{CurrentUser}' AND xtype='U')
                                            CREATE TABLE {CurrentUser} (Date date, TimeIn time, TimeOut time, Details text)"));
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
                    MessageBox.Show("Push successful!");
                }
            }).Start();
        }

        public void RemoveWeek(DateTime date)
        {
            if (!SqlEnabled) return;
            Debug.WriteLine("Remove week");
            var datesInWeek = DatesInWeek(date);
            AddCommand(new SerilizeSqlCommand($@"DELETE FROM {CurrentUser} WHERE( Date = '" + datesInWeek[0] + "' OR Date = '" + datesInWeek[1] + "' OR Date = '" + datesInWeek[2] + "' OR Date = '" + datesInWeek[3] + "' OR Date = '" + datesInWeek[4] +
                                      "' OR Date = '" + datesInWeek[5] + "' OR Date = '" + datesInWeek[6] + "')"));
        }

        /// <summary>
        /// Inserts day into data base
        /// </summary>
        /// <param name="day"></param>
        public void InsertDay(Day day)
        {
            if (!SqlEnabled) return;
            Debug.WriteLine("Insert day");
            if (day == null) return;
            try
            {
                using (SerilizeSqlCommand command = new SerilizeSqlCommand($"INSERT INTO {CurrentUser} VALUES(@Date, @TimeIn, @TimeOut, @Details)"))
                {
                    command.AddParameter(new SqlParameter("Date", day.Date));
                    command.AddParameter(new SqlParameter("TimeIn", new DateTime().TimeOfDay));
                    command.AddParameter(new SqlParameter("TimeOut", new DateTime().TimeOfDay));
                    command.AddParameter(new SqlParameter("Details", day.Details));
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
            if (!SqlEnabled) return;
            Debug.WriteLine("Remove day");
            var command = new SerilizeSqlCommand($@"DELETE FROM {CurrentUser} WHERE( Date = '{date}' )");
            AddCommand(command);
        }

        public void InsertTime(Time time)
        {
            if (!SqlEnabled) return;
            Debug.WriteLine("Insert time");
            if (time == null) return;
            try
            {
                using (SerilizeSqlCommand command = new SerilizeSqlCommand($"INSERT INTO {CurrentUser} VALUES(@Date, @TimeIn, @TimeOut, @Details)"))
                {
                    command.AddParameter(new SqlParameter("Date", time.TimeIn.Date));
                    command.AddParameter(new SqlParameter("TimeIn", time.TimeIn.TimeOfDay));
                    command.AddParameter(new SqlParameter("TimeOut", time.TimeOut.TimeOfDay));
                    command.AddParameter(new SqlParameter("Details", ""));
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
            if (!SqlEnabled) return;
            Debug.WriteLine("Remove time");
            SerilizeSqlCommand command = new SerilizeSqlCommand($@"DELETE FROM {CurrentUser} WHERE( Date = '{time.TimeIn.Date}' AND TimeIn = '{time.TimeIn.TimeOfDay}' AND TimeOut = '{time.TimeOut.TimeOfDay}')");
            AddCommand(command);
        }

        public void UpdateDetails(Day day)
        {
            if (!SqlEnabled) return;
            Debug.WriteLine("Update details");
            if (day.Details == "" && day.Times.Count == 0) // day is removed in local data if no time and details == ""
            {
                RemoveDay(day.Date);
            }
            else
            {
                using (SerilizeSqlCommand cmd = new SerilizeSqlCommand($"UPDATE {CurrentUser} SET Details = @Details WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new TimeSpan() + "')"))
                {
                    cmd.AddParameter(new SqlParameter("Details", day.Details));
                    cmd.Type = SerilizeSqlCommand.CommandType.UpdateDetails;
                    
                    AddCommand(cmd);
                }
            }
        }

        public void SqlUpdateTime(Time prev, Time upd)
        {
            if (!SqlEnabled) return;
            Debug.WriteLine("Update time");
            using (SerilizeSqlCommand cmd = new SerilizeSqlCommand($"UPDATE {CurrentUser} SET Date = '{upd.TimeIn.Date}', TimeIn = '{upd.TimeIn.TimeOfDay}', TimeOut = '{upd.TimeOut.TimeOfDay}' WHERE( Date = '" + prev.TimeIn.Date + "' AND TimeIn = '" +prev.TimeIn.TimeOfDay + "' AND TimeOut = '" + prev.TimeOut.TimeOfDay + "')"))
            {
                cmd.Type = SerilizeSqlCommand.CommandType.UpdateTime;
               
                AddCommand(cmd);
            }
        }

        #endregion

        private void OnTimeDataUpdate(List<Day> days)
        {
            if (!SqlEnabled) return;
            Debug.WriteLine("SQL server time data update");
            TimeDateaUpdate?.Invoke(days); // Fire event for updating ui
        }

        /// <summary>
        /// Takes a full user data table name and returns his days from server
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public List<Day> Load(string user)
        {
            if (!SqlEnabled || SqlConnection == null) return null;
            var days = new List<Day>();
            lock (SqlServerLock)
            {
                Debug.WriteLine("Loading data from server");
                DataTable temp = new DataTable();
                try
                {
                    string query = $"SELECT * FROM {user}";
                        using (SqlCommand cmd = new SqlCommand(query, SqlConnection))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(temp);
                            }
                        }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    Console.WriteLine(e);
                }

                try
                {
                    for (int i = 0; i < temp.Rows.Count; i++)
                    {
                        DataRow row = temp.Rows[i];
                        if (row.ItemArray[1] is TimeSpan span)
                        {
                            if (span == new TimeSpan()) // if this is a day header
                            {
                                if (row.ItemArray[0] is DateTime time)
                                {
                                    if (!days.Exists(d => d.Date == (DateTime) row.ItemArray[0]))
                                    {
                                        days.Add(new Day(time) {Details = row.ItemArray[3].ToString()});
                                    }
                                }
                            }
                        }

                        ProgressChangedEvent?.Invoke(100f / temp.Rows.Count / 2 * i);
                    }

                    foreach (Day day in days)
                    {
                        foreach (DataRow row in temp.Rows)
                        {
                            if (row.ItemArray[0] is DateTime time && row.ItemArray[1] is TimeSpan intime && row.ItemArray[2] is TimeSpan outtime)
                            {
                                if (time == day.Date)
                                {
                                    if (intime != new TimeSpan()) // if this is not a day header
                                    {
                                        day.Times.Add(new Time(new DateTime(day.Date.Year, day.Date.Month, day.Date.Day, intime.Hours, intime.Minutes, intime.Seconds),
                                            new DateTime(day.Date.Year, day.Date.Month, day.Date.Day, outtime.Hours, outtime.Minutes, outtime.Seconds)));
                                    }
                                }
                            }
                        }
                    }

                    ProgressChangedEvent?.Invoke(100);
                    days = days.OrderBy(d => d.Date).ToList();
                    OnTimeDataUpdate(days);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    Console.WriteLine(e);
                }
            }

            return days;
        }

        /// <summary>
        /// Returns timedata form server.
        /// Calls TimeDataUpdate event with data as arg.
        /// </summary>
        /// <returns></returns>
        public List<Day> LoadCurrentUser()
        {
            if (!SqlEnabled)
            {
                MessageBox.Show("SQL not enabled!");
                return null;
            }
            return Load(CurrentUser);
        }

        #region pull data cyclic read

        private void PullDataElapsed(object sender, ElapsedEventArgs e)
        {
            if (!SqlEnabled) return;
            Debug.WriteLine("Pull data eleapsed.");
            PullData();
            StartCyclicSqlRead();
        }

        /// <summary>
        /// Pulls current user days from SQL
        /// </summary>
        public void PullData()
        {
            if (!SqlEnabled || SqlConnection == null) return;
            Debug.WriteLine("Pull data from server");
            lock (SqlPullLock) // must use seperate lock case sqlserverlock is used in loaddatafromserver()
            {
                try
                {
                    string query = $"SELECT * FROM {CurrentUser}";
                    using (SqlCommand cmd = new SqlCommand(query, SqlConnection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable temp = new DataTable();
                            da.Fill(temp);
                            if (AreTablesTheSame(temp, _dataTable))
                            {
                                return;
                            }

                            _dataTable = temp.Copy();
                            LoadCurrentUser();
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    Console.WriteLine(e);
                }
            }
        }

        #endregion

        private static bool AreTablesTheSame(DataTable tbl1, DataTable tbl2)
        {
            if (tbl1.Rows.Count != tbl2.Rows.Count || tbl1.Columns.Count != tbl2.Columns.Count) return false;
            for (int i = 0; i < tbl1.Rows.Count; i++)
            {
                for (int c = 0; c < tbl1.Columns.Count; c++)
                {
                    if (!Equals(tbl1.Rows[i][c], tbl2.Rows[i][c])) return false;
                }
            }

            return true;
        }
    }
}