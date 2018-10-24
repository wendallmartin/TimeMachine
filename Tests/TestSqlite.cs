using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using TheTimeApp;
using TheTimeApp.TimeData;
using Day = TheTimeApp.TimeData.Day;

namespace Tests
{
    [TestFixture]
    public class TestSqlite
    {
        private Sqlite _instance;
        private string user = "1_ - 3~988}.wr.*@%'?>sS";
        
        [SetUp]
        public void Setup()
        {
            TimeServer.SqlCurrentUser = user;
            AppSettings.Instance = new AppSettings();
            AppSettings.Instance.DataPath = "TimeData.sqlite";
            _instance = Sqlite.LoadFromFile();
            _instance.AddUser(new User(user, "", new List<Day>()));
            TimeServer.SqlCurrentUser = user;
        }

        [TearDown]
        public void TearDown()
        {
            _instance.Dispose();
            
            if(File.Exists($"{AppSettings.Instance.DataPath}"))
                File.Delete($"{AppSettings.Instance.DataPath}");
        }

        [Test]
        public void Test_UserNames()
        {
            Assert.True(_instance.UserNames().Count == 1);
            Assert.True(_instance.UserNames().Contains(user));
            _instance.AddUser(new User("Test", "", new List<Day>()));
            Assert.True(_instance.UserNames().Count == 2);
            Assert.True(_instance.UserNames().Contains("Test"));
        }

        [Test]
        public void Test_IsClockedIn()
        {
            string key = TimeServer.GenerateId();
            Assert.False(_instance.IsClockedIn());
            _instance.PunchIn(key);
            Assert.True(_instance.IsClockedIn());
            Thread.Sleep(100);
            _instance.PunchOut(key);
            Assert.False(_instance.IsClockedIn());
        }

        [Test]
        public void Test_SqlCurrentUser()
        {
            Assert.True(TimeServer.SqlCurrentUser == user);
            _instance.AddUser(new User("tester", "", new List<Day>()));
            TimeServer.SqlCurrentUser = "tester";
            Assert.True(TimeServer.SqlCurrentUser == "tester");
        }
        
        [Test]
        public void Test_AddUser()
        {

            string name = "lgahjasfhfsdjfghlasjl";
            Assert.False(_instance.UserNames().Contains(name));
            _instance.AddUser(new User(name, "", new List<Day>()));
            Assert.True(_instance.UserNames().Contains(name));
        }

        [Test]
        public void Test_CurrentDay()
        {
            Assert.True(_instance.CurrentDay().Date == DateTime.Now.Date);
        }

        [Test]
        public void Test_DeleteTime()
        {
            string key = TimeServer.GenerateId();
            _instance.PunchIn(key);
            _instance.PunchOut(key);
            Assert.True(_instance.AllTimes().Count == 1);
            Time time = _instance.AllTimes().First();
            _instance.DeleteTime(time.Key);
            Assert.True(_instance.AllTimes().Count == 0);
        }

        [Test]
        public void Test_AddDay()
        {
            Assert.False(_instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
            _instance.AddDay(new Day(DateTime.Now.Date));
            Assert.True(_instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
        }

        [Test]
        public void Test_DeleteDay()
        {
            Assert.False(_instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
            _instance.AddDay(new Day(DateTime.Now.Date));
            Assert.True(_instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
            _instance.DeleteDay(DateTime.Now.Date);
            Assert.False(_instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
        }

        [Test]
        public void Test_DeleteRange()
        {
            DateTime current = _instance.CurrentDay().Date;
            DateTime wayBack = new DateTime(2007, 6, 5);
            _instance.AddDay(new Day(wayBack));
            var days = _instance.AllDays();
            Assert.True(days.Any(d => d.Date == current));
            Assert.True(days.Any(d => d.Date == wayBack));
            _instance.DeleteRange(wayBack.AddDays(1), current);
            days = _instance.AllDays();
            foreach (Day day in days)
            {
                Assert.False(day.Date == current);
            }
            Assert.True(days.Any(d => d.Date == wayBack));
            _instance.DeleteRange(new DateTime(7, 6, 5), new DateTime(7, 6, 6));
            days = _instance.AllDays();
            Assert.False(days.Any(d => d.Date.Year == 2005 && d.Date.Month == 7 && d.Date.Day == 6));
        }

        [Test]
        public void Test_DeleteUser()
        {
            _instance.DeleteUser(user);
            Assert.True(_instance.UserNames().Count == 0);
        }

        [Test]
        public void Test_UpdateDetails()
        {
            _instance.AddDay(new Day(DateTime.Now.Date));
            _instance.UpdateDetails(DateTime.Now.Date, "Hello");
            Assert.True(_instance.AllDays().Last().Details == "Hello");
            _instance.UpdateDetails(DateTime.Now.Date, "GoodBy");
            Assert.True(_instance.AllDays().Last().Details == "GoodBy");
        }

        [Test]
        public void Test_UpdateTime()
        {
            string key = TimeServer.GenerateId();
            _instance.PunchIn(key);
            _instance.PunchOut(key);
            DateTime now = DateTime.Now;
            _instance.UpdateTime(key, new Time(){TimeIn = now, TimeOut = DateTime.MaxValue});
            Time last = _instance.AllTimes().Last();
            Assert.True(last.TimeIn.ToString(CultureInfo.InvariantCulture) == now.ToString(CultureInfo.InvariantCulture));
            Assert.True(last.TimeOut.ToString(CultureInfo.InvariantCulture) == DateTime.MaxValue.ToString(CultureInfo.InvariantCulture));
        }

        [Test]
        public void Test_MinDate()
        {
            _instance.AddDay(new Day(new DateTime(2009,4,25)));
            _instance.AddDay(new Day(new DateTime(2010,4,25)));
            _instance.AddDay(new Day(new DateTime(2011,4,25)));
            
            Assert.True(_instance.MinDate() == new DateTime(2009,4,25));
        }
        
        [Test]
        public void Test_MaxDate()
        {
            _instance.AddDay(new Day(new DateTime(2009,4,25)));
            _instance.AddDay(new Day(new DateTime(2010,4,25)));
            _instance.AddDay(new Day(new DateTime(2011,4,25)));
            
            Assert.True(_instance.MaxDate() == new DateTime(2011,4,25));
        }


        [Test]
        public void Test_PunchIn()
        {
            string key = TimeServer.GenerateId();
            _instance.PunchIn(key);
            Time last = _instance.AllTimes().Last();
            Assert.True(last.TimeIn.ToString(CultureInfo.InvariantCulture) == last.TimeOut.ToString(CultureInfo.InvariantCulture));
            Assert.True(last.TimeIn.Millisecond == last.TimeOut.Millisecond);
        }

        [Test]
        public void Test_PunchOut()
        {
            string key = TimeServer.GenerateId();
            _instance.PunchIn(key);
            Thread.Sleep(100);
            _instance.PunchOut(key);
            Time last = _instance.AllTimes().Last();
            Assert.True(last.TimeIn.Millisecond != last.TimeOut.Millisecond);
        }

        [Test]
        public void Test_StartEndWeek()
        {
            Assert.True(TimeServer.StartEndWeek(DateTime.Now)[0].DayOfWeek == DayOfWeek.Sunday && TimeServer.StartEndWeek(DateTime.Now)[1].DayOfWeek == DayOfWeek.Saturday);
            Assert.True((TimeServer.StartEndWeek(DateTime.Now)[1].DayOfYear - TimeServer.StartEndWeek(DateTime.Now)[0].DayOfYear ) == 6);
        }
        
        [Test]
        public void Test_PushPull()
        {
            Day day1 = new Day(new DateTime(2001, 11, 7, 9, 44, 15, 411));
            List<Time> times1 = new List<Time>();
            times1.Add(new Time(new DateTime(2001, 11, 7, 9, 44, 15, 411), new DateTime(2001, 11, 7, 12, 3, 1, 411)){Key = TimeServer.GenerateId()});
            times1.Add(new Time(new DateTime(2001, 11, 7, 1, 00, 00, 200), new DateTime(2001, 11, 7, 3, 00, 1, 999)){Key = TimeServer.GenerateId()});
            day1.Times = times1;
            day1.Details = "does this even work any more?";
            
            Day day2 = new Day(new DateTime(2051, 11, 6, 8, 44, 15, 411));
            List<Time> times2 = new List<Time>();
            times2.Add(new Time(new DateTime(2051, 11, 6, 8, 44, 15, 411), new DateTime(2051, 11, 6, 12, 30, 1, 100)){Key = TimeServer.GenerateId()});
            times2.Add(new Time(new DateTime(2051, 11, 6, 10, 4, 15, 411), new DateTime(2051, 11, 6, 11, 44, 15, 411)){Key = TimeServer.GenerateId()});
            day2.Times = times2;
            day2.Details = "lets try again.....";
            
            Day day3 = new Day(new DateTime(2021, 4, 3, 5, 5, 5, 888));
            List<Time> times3 = new List<Time>();
            times3.Add(new Time(new DateTime(2021, 4, 3, 5, 5, 5, 888), new DateTime(2021, 4, 3, 8, 5, 9, 800)){Key = TimeServer.GenerateId()});
            times3.Add(new Time(new DateTime(2021, 4, 3, 1, 5, 9, 800), new DateTime(2021, 4, 3, 6, 55, 9, 800)){Key = TimeServer.GenerateId()});
            day3.Times = times3;
            day3.Details = "one more time!";

            var push = new List<Day>() {day1, day2, day3 };
            _instance.Push(push);
            var pull = _instance.Pull();
            foreach (Day day in push)
            {
                bool contains = false;
                foreach (Day d in pull)
                {
                    if (Day.Equals(day, d))
                    {
                        contains = true;
                    }       
                }
                Assert.IsTrue(contains);
            }
            Assert.IsTrue(push.Count == pull.Count);
        }
        
        [Test]
        public void Test_FixVersionMismatches()
        {
            TimeServer.SqlCurrentUser = "TestFixMisMatch";
            DateTime dateTime = DateTime.Now;
            string dateString = TimeServer.DateSqLite(dateTime);
            string dateTimeString = TimeServer.DateTimeSqLite(dateTime);
            using (SQLiteCommand cmd = _instance.GetConnection.CreateCommand())
            {
                cmd.CommandText = $"DROP TABLE IF EXISTS [{_instance.DayTableName}]";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $"DROP TABLE IF EXISTS [{_instance.TimeTableName}]";
                cmd.ExecuteNonQuery();
            }
            using (SQLiteCommand cmd = _instance.GetConnection.CreateCommand())
            {
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS [{_instance.DayTableName}] ( [Date] TEXT, [Details] TEXT)";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{_instance.TimeTableName}]([Date] TEXT, [TimeIn] TEXT, [TimeOut] TEXT, [Key] INT)";
                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmd = _instance.GetConnection.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO [{_instance.DayTableName}] VALUES(@Date, @Details)";
                cmd.Parameters.Add(new SQLiteParameter("Date", dateString));
                cmd.Parameters.Add(new SQLiteParameter("Details", "testing....."));
                cmd.ExecuteNonQuery();
            }
            using (SQLiteCommand cmd = _instance.GetConnection.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO [{_instance.TimeTableName}] VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                cmd.Parameters.Add(new SQLiteParameter("Date", dateString));
                cmd.Parameters.Add(new SQLiteParameter("TimeIn", dateTimeString));
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", dateTimeString));
                cmd.Parameters.Add(new SQLiteParameter("Key", 6));
                cmd.ExecuteNonQuery();
            }
            using (SQLiteCommand cmd = _instance.GetConnection.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO [{_instance.TimeTableName}] VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                cmd.Parameters.Add(new SQLiteParameter("Date", dateString));
                cmd.Parameters.Add(new SQLiteParameter("TimeIn", dateTimeString));
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", dateTimeString));
                cmd.Parameters.Add(new SQLiteParameter("Key", 8));
                cmd.ExecuteNonQuery();
            }
            using (SQLiteCommand cmd = _instance.GetConnection.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO [{_instance.TimeTableName}] VALUES(@Date, @TimeIn, @TimeOut, @Key)";
                cmd.Parameters.Add(new SQLiteParameter("Date", dateString));
                cmd.Parameters.Add(new SQLiteParameter("TimeIn", dateTimeString));
                cmd.Parameters.Add(new SQLiteParameter("TimeOut", dateTimeString));
                cmd.Parameters.Add(new SQLiteParameter("Key", 7));
                cmd.ExecuteNonQuery();
            }
            
            var days = _instance.AllDays();
            Assert.IsTrue(days[0].Times.Count == 3);
            _instance.FixVersionMismatches();

            days = _instance.AllDays();
            Assert.IsTrue(days[0].Times.Count == 3);
            foreach (Day day in days)
            {
                Assert.IsTrue(day.Details == "testing.....");
                Assert.IsTrue(day.Date.ToString() == dateTime.Date.ToString());
                foreach (Time time in day.Times)
                {
                    Assert.IsTrue(time.TimeIn.ToString() == dateTime.ToString());
                    Assert.IsTrue(time.TimeOut.ToString() == dateTime.ToString());
                    Assert.IsTrue(time.Key.Length > 1);
                }
            }
        }
    }
}