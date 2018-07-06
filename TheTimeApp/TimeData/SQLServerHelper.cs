using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
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
        private static readonly object SqlServerLock = new object();
        
        private static readonly object SqlPullLock = new object();

        public delegate void ProgressChangedDel(float value);

        public delegate void ConnectionChangedDel(bool value);

        public delegate void ProgressFinishDel();

        public delegate void TimeDateUpdated(List<Day> data);

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
        private DataTable _dataTable = new DataTable();

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

        private static SqlConnectionStringBuilder ConnectionStringBuilder =>
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
            if (AppSettings.MainPermission == "write" && AppSettings.SQLEnabled == "true")
            {
                new Thread(TestConnection).Start();    
            }

            // only start sql cyclic read if sql enabled and MainPermission is 'read'
            if ( AppSettings.SQLEnabled == "true" && AppSettings.MainPermission == "read" && IsConnected)
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

        public static bool IsConnected
        {
            get{
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
        }

        private void OnConnectionChanged(bool connected)
        {
            ConnectionChangedEvent?.Invoke(connected);
        }
        
        #endregion

        #region SQL message pump
        
        public List<string> GetAllTables()
        {
            if (AppSettings.SQLEnabled != "true")
                return null;
            
            var tables = new List<string>();

            using (SqlConnection connection = new SqlConnection(ConnectionStringBuilder.ConnectionString))
            {
                connection.Open();
                DataTable names = connection.GetSchema("Tables");
                foreach (DataRow dataRow in names.Rows)
                {
                    string tableName = dataRow[2].ToString();
                    if (tableName.Contains("_TimeTable"))
                    {
                        tableName = tableName.Replace("_TimeTable", "");
                        tables.Add(tableName);
                    }
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
            if (AppSettings.SQLEnabled != "true")
                return;
            
            AddCommand(new SqlCommand($@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{ToTableName(user)}' AND xtype='U')
                                            CREATE TABLE {ToTableName(user)} (Date date, TimeIn time, TimeOut time, Details text)"));
        }
        
        
        
        /// <summary>
        /// Deletes user table if it exist
        /// EXCEPTS USER NAME NOT USER TABLE NAME!!
        /// </summary>
        /// <param name="username"></param>
        public void RemoveUser(string username)
        {
            if (AppSettings.SQLEnabled != "true")
                return;
            
            AddCommand(new SqlCommand($@"IF EXISTS (SELECT * FROM sysobjects WHERE name='{ToTableName(username)}' AND xtype='U') DROP TABLE {ToTableName(username)}"));            
        }

        
        /// <summary>
        /// Add command to list and trys to flush commands
        /// </summary>
        /// <param name="command"></param>
        private void AddCommand(SqlCommand command)
        {
            if (AppSettings.SQLEnabled != "true") return;
            new Thread(() =>
            {
                lock (SqlServerLock)
                {
                    UpdateChangedEvent?.Invoke(false);
                    _commands.Add(command);
                    FlushCommands();
                }
            }).Start();
        }

        private void FlushCommands()
        {
            try
            {
                var successful = new List<SqlCommand>();
                using (SqlConnection connection = new SqlConnection(ConnectionStringBuilder.ConnectionString))
                {
                    connection.Open();
                    foreach (SqlCommand c in _commands)
                    {
                        UpdateChangedEvent?.Invoke(false);
                        SqlCommand sqlCommand = c;
                        sqlCommand.Connection = connection;
                        int value = sqlCommand.ExecuteNonQuery();
                        Debug.WriteLine($"Excecute: {value}");
                        successful.Add(c);
                    }
                }

                // there is no exceptions
                foreach (SqlCommand success in successful)
                {
                    _commands.Remove(success);
                }

                TimeData.TimeDataBase.Commands = _commands;
                UpdateChangedEvent?.Invoke(_commands.Count == 0);
            }
            catch (Exception e)
            {
                using (StreamWriter logger = new StreamWriter($"log\\{DateTime.Now.Date}sqlerrors.log", true))
                {
                    logger.WriteLine($"{DateTime.Now}: {e.Message}");
                }
            }
        }

        #endregion

        #region Message generators

        #region unused

        private bool ServerContainsDay(Day day)
        {
            if (AppSettings.SQLEnabled != "true")
                return false;
            
            try
            {
                lock (SqlServerLock)
                {
                    using (SqlConnection con = new SqlConnection(ConnectionStringBuilder.ConnectionString))
                    {
                        con.Open();
                        using (SqlCommand command = new SqlCommand($@"SELECT * FROM {CurrentUser} WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new DateTime().TimeOfDay + "')", con))
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
            if (AppSettings.SQLEnabled != "true")
                return false;
            try
            {
                using (SqlCommand command = new SqlCommand($@"SELECT * FROM {CurrentUser} WHERE( Date = @Date AND TimeIn = @TimeIn AND 
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
            if (AppSettings.SQLEnabled != "true")
                return;
            
            lock (SqlServerLock)
            {
                Days = days;

                // removes deleted from server
                SqlCommand dayCommand = new SqlCommand($"SELECT * FROM {CurrentUser} WHERE (TimeIn = '" + new TimeSpan() + "' AND TimeOut = '" + new TimeSpan() + "')");// todo, must add connection using statement
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

                SqlCommand timeCommand = new SqlCommand($"SELECT * FROM {CurrentUser} WHERE (TimeIn <> '" + new TimeSpan() + "' AND TimeOut <> '" + new TimeSpan() + "')");// todo must add connection using statement
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
                            using (SqlCommand command = new SqlCommand($"DELETE FROM {CurrentUser} WHERE( TimeIn = '" + (TimeSpan) timeReader["TimeIn"] + "' AND TimeOut = '" + (TimeSpan) timeReader["TimeOut"] + "')"))
                            {
                                AddCommand(command);
                            }
                        }
                    }
                }
            }
        }
        

        #endregion
        
        /// <summary>
        /// Delete sql table and repushes everything
        /// </summary>
        /// <param name="days"></param>
        public void RePushToServer(List<Day> days)
        {
            if (AppSettings.SQLEnabled != "true")
            {
                MessageBox.Show("SQL not enabled!");
                return;
            }
            new Thread(() =>
            {
                lock (SqlServerLock)
                {
                    AddCommand(new SqlCommand($@"IF EXISTS (SELECT * FROM sysobjects WHERE name='{CurrentUser}' AND xtype='U') DROP TABLE {CurrentUser}"));
                    AddCommand(new SqlCommand($@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{CurrentUser}' AND xtype='U')
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
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Remove week");
            var datesInWeek = DatesInWeek(date);
            AddCommand(new SqlCommand($@"DELETE FROM {CurrentUser} WHERE( Date = '" + datesInWeek[0] + "' OR Date = '" + datesInWeek[1] + "' OR Date = '" + datesInWeek[2] + "' OR Date = '" + datesInWeek[3] + "' OR Date = '" + datesInWeek[4] +
                                      "' OR Date = '" + datesInWeek[5] + "' OR Date = '" + datesInWeek[6] + "')"));
        }
        
        /// <summary>
        /// Inserts day into data base
        /// </summary>
        /// <param name="day"></param>
        public void InsertDay(Day day)
        {
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Insert day");
            if (day == null) return;
            try
            {
                using (SqlCommand command = new SqlCommand($"INSERT INTO {CurrentUser} VALUES(@Date, @TimeIn, @TimeOut, @Details)"))
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
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Remove day");
            AddCommand(new SqlCommand($@"DELETE FROM {CurrentUser} WHERE( Date = '" + date + "')"));
        }

        /// <summary>
        /// Just removes the day row.
        /// </summary>
        /// <param name="day"></param>
        public void RemoveDayHeader(Day day)
        {
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Remove day header");
            AddCommand(new SqlCommand($"DELETE FROM {CurrentUser} WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new TimeSpan() + "')"));
        }
        
        public void InsertTime(Time time)
        {
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Insert time");
            if (time == null) return;
            try
            {
                using (SqlCommand command = new SqlCommand($"INSERT INTO {CurrentUser} VALUES(@Date, @TimeIn, @TimeOut, @Details) "))
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
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Remove time");
            SqlCommand command = new SqlCommand($@"DELETE FROM {CurrentUser} WHERE( Date = '{time.TimeIn.Date}' AND TimeIn = '{time.TimeIn.TimeOfDay}' AND TimeOut = '{time.TimeOut.TimeOfDay}')");
            AddCommand(command);
        }

        public void UpdateDetails(Day day)
        {
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Update details");
            if (day.Details == "" && day.Times.Count == 0)// day is removed in local data if no time and details == ""
            {
                RemoveDayHeader(day);
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand($"UPDATE {CurrentUser} SET Details = @Details WHERE( Date = '" + day.Date + "' AND TimeIn = '" + new TimeSpan() + "')"))
                {
                    cmd.Parameters.AddWithValue("@Details", day.Details);
                    AddCommand(cmd);
                }                    
            }
        }
        
        public void SqlUpdateTime(Time prev, Time upd)
        {
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Update time");
            using (SqlCommand cmd = new SqlCommand($"UPDATE {CurrentUser} SET Date = @Date, TimeIn = @TimeIn, TimeOut = @TimeOut WHERE( Date = '" + prev.TimeIn.Date + "' AND TimeIn = '" + prev.TimeIn.TimeOfDay + "' AND TimeOut = '" + prev.TimeOut.TimeOfDay + "')"))
            {
                cmd.Parameters.AddWithValue("@Date", upd.TimeIn.Date);
                cmd.Parameters.AddWithValue("@TimeIn", upd.TimeIn.TimeOfDay);
                cmd.Parameters.AddWithValue("@TimeOut", upd.TimeOut.TimeOfDay);
                AddCommand(cmd);
            }   
        }
        
        #endregion

        private void OnTimeDataUpdate(List<Day> days)
        {
            if (AppSettings.SQLEnabled != "true")
                return;
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
            if (AppSettings.SQLEnabled != "true")
                return null;
            
            var days = new List<Day>();
            lock (SqlServerLock)
            {
                Debug.WriteLine("Loading data from server");
                DataTable temp = new DataTable();
                try
                {
                    string query = $"SELECT * FROM {user}";
                    using (SqlConnection conn = new SqlConnection(ConnectionStringBuilder.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(temp);
                            }
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
                        if (row.ItemArray[1] is TimeSpan)
                        {
                            if (((TimeSpan) row.ItemArray[1]) == new TimeSpan()) // if this is a day header
                            {
                                if (row.ItemArray[0] is DateTime)
                                {
                                    if (!days.Exists(d => d.Date == (DateTime) row.ItemArray[0]))
                                    {
                                        days.Add(new Day((DateTime) row.ItemArray[0]) {Details = row.ItemArray[3].ToString()});
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
            if (AppSettings.SQLEnabled != "true")
            {
                MessageBox.Show("SQL not enabled!");
                return null;
            }
            return Load(CurrentUser);
        }

        #region pull data cyclic read

        private void PullDataElapsed(object sender, ElapsedEventArgs e)
        {
            if (AppSettings.SQLEnabled != "true")
                return;
            
            Debug.WriteLine("Pull data eleapsed.");
            PullData();
            StartCyclicSqlRead();
        }
        
        /// <summary>
        /// Pulls current user days from SQL
        /// </summary>
        public void PullData()
        {
            if (AppSettings.SQLEnabled != "true")
                return;
            
            if (!IsConnected)
                return;
            
            Debug.WriteLine("Pull data from server");
            lock (SqlPullLock)// must use seperate lock case sqlserverlock is used in loaddatafromserver()
            {
                try
                {
                    string query = $"SELECT * FROM {CurrentUser}";

                    using (SqlConnection conn = new SqlConnection(ConnectionStringBuilder.ConnectionString))
                    {
                        conn.Open();

                        using (SqlCommand cmd = new SqlCommand(query, conn))
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
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    Console.WriteLine(e);
                }
            }
        }
        
        #endregion
        
        private static bool AreTablesTheSame( DataTable tbl1, DataTable tbl2)
        {
            if (tbl1.Rows.Count != tbl2.Rows.Count || tbl1.Columns.Count != tbl2.Columns.Count)
                return false;


            for ( int i = 0; i < tbl1.Rows.Count; i++)
            {
                for ( int c = 0; c < tbl1.Columns.Count; c++)
                {
                    if (!Equals(tbl1.Rows[i][c] ,tbl2.Rows[i][c]))
                        return false;
                }
            }
            return true;
        }
    }
}