﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using TheTimeApp;
using TheTimeApp.TimeData;
using Day = TheTimeApp.TimeData.Day;

namespace Tests
{
    [TestFixture]
    public class TestMySql
    {
        private TheTimeApp.TimeData.MySql _instance;
        [SetUp]
        public void Setup()
        {
            AppSettings.Validate();
            AppSettings.MySqlDatabase = "test";
            TimeServer.SqlCurrentUser = "nunit";
            MySqlConnectionStringBuilder mysqlBuiler = new MySqlConnectionStringBuilder()
            {
                Server = "localhost",
                UserID = "WendallMartin",
                Password = "test",
                Database = "test",
                Port = 3306
            };
            _instance = new TheTimeApp.TimeData.MySql(mysqlBuiler.ConnectionString);
            _instance.ClearDataBase("test");
            _instance.AddUser(new User("nunit", "", new List<Day>()));
            TimeServer.SqlCurrentUser = "nunit";
        }

        [TearDown]
        public void TearDown()
        {
            _instance.Dispose();
        }

        [Test]
        public void Test_UserNames()
        {
            Assert.True(_instance.UserNames().Count == 1);
            Assert.True(_instance.UserNames().Contains("nunit"));
            _instance.AddUser(new User("Test", "", new List<Day>()));
            Assert.True(_instance.UserNames().Count == 2);
            Assert.True(_instance.UserNames().Contains("Test"));
        }

        [Test]
        public void Test_IsClockedIn()
        {
            Assert.False(_instance.IsClockedIn());
            _instance.PunchIn();
            Assert.True(_instance.IsClockedIn());
            Thread.Sleep(100);
            _instance.PunchOut();
            Assert.False(_instance.IsClockedIn());
        }

        [Test]
        public void Test_SqlCurrentUser()
        {
            Assert.True(TimeServer.SqlCurrentUser == "nunit");
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
            var names = _instance.UserNames();
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
            _instance.PunchIn();
            _instance.PunchOut();
            Assert.True(_instance.AllTimes().Count == 1);
            Time time = _instance.AllTimes().First();
            int result = _instance.DeleteTime(time.Key);
            Assert.True(result == 1);
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
            _instance.DeleteUser("nunit");
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
            _instance.PunchIn();
            _instance.PunchOut();
            DateTime now = DateTime.Now;
            Assert.True(_instance.UpdateTime(1, new Time(){TimeIn = now, TimeOut = DateTime.MaxValue}) == 1);
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
            Assert.True(Math.Abs(_instance.MaxTimeId()) < .0001);
            _instance.PunchIn();
            Time last = _instance.AllTimes().Last();
            Assert.True(last.TimeIn.ToString(CultureInfo.InvariantCulture) == last.TimeOut.ToString(CultureInfo.InvariantCulture));
            Assert.True(last.TimeIn.Millisecond == last.TimeOut.Millisecond);
            Assert.True(Math.Abs(_instance.MaxTimeId() - 1) < .0001);
        }

        [Test]
        public void Test_PunchOut()
        {
            Assert.True(Math.Abs(_instance.MaxTimeId()) < .0001);
            _instance.PunchIn();
            Thread.Sleep(100);
            _instance.PunchOut();
            Time last = _instance.AllTimes().Last();
            Assert.True(last.TimeIn.Millisecond != last.TimeOut.Millisecond);
            
        }

        [Test]
        public void Test_StartEndWeek()
        {
            Assert.True(TimeServer.StartEndWeek(DateTime.Now)[0].DayOfWeek == DayOfWeek.Sunday && TimeServer.StartEndWeek(DateTime.Now)[1].DayOfWeek == DayOfWeek.Saturday);
            Assert.True((TimeServer.StartEndWeek(DateTime.Now)[1].DayOfYear - TimeServer.StartEndWeek(DateTime.Now)[0].DayOfYear ) == 6);
        }
    }
}