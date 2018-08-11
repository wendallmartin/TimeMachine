using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using TheTimeApp.TimeData;
using Day = TheTimeApp.TimeData.Day;

namespace Tests
{
    [TestFixture]
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            LocalSql.Instance = new LocalSql("Data Source=TimeData.sqlite;Version=3");
            LocalSql.Instance.AddUser(new User("nunit", "", new List<Day>()));
            LocalSql.Instance.SqlCurrentUser = "nunit";
        }

        [TearDown]
        public void TearDown()
        {
            LocalSql.Instance.Dispose();
            
            if(File.Exists("TimeData.sqlite"))
                File.Delete("TimeData.sqlite");
        }

        [Test]
        public void Test_UserNames()
        {
            Assert.True(LocalSql.Instance.UserNames.Count == 1);
            Assert.True(LocalSql.Instance.UserNames.Contains("nunit"));
            LocalSql.Instance.AddUser(new User("Test", "", new List<Day>()));
            Assert.True(LocalSql.Instance.UserNames.Count == 2);
            Assert.True(LocalSql.Instance.UserNames.Contains("Test"));
        }

        [Test]
        public void Test_IsClockedIn()
        {
            Assert.False(LocalSql.Instance.IsClockedIn());
            LocalSql.Instance.PunchIn();
            Assert.True(LocalSql.Instance.IsClockedIn());
            Thread.Sleep(100);
            LocalSql.Instance.PunchOut();
            Assert.False(LocalSql.Instance.IsClockedIn());
        }

        [Test]
        public void Test_SqlCurrentUser()
        {
            Assert.True(LocalSql.Instance.SqlCurrentUser == "nunit");
            LocalSql.Instance.AddUser(new User("tester", "", new List<Day>()));
            LocalSql.Instance.SqlCurrentUser = "tester";
            Assert.True(LocalSql.Instance.SqlCurrentUser == "tester");
        }
        
        [Test]
        public void Test_AddUser()
        {

            string name = "lgahjasfhfsdjfghlasjl";
            Assert.False(LocalSql.Instance.UserNames.Contains(name));
            LocalSql.Instance.AddUser(new User(name, "", new List<Day>()));
            Assert.True(LocalSql.Instance.UserNames.Contains(name));
        }

        [Test]
        public void Test_CurrentDay()
        {
            Assert.True(LocalSql.Instance.CurrentDay().Date == DateTime.Now.Date);
        }

        [Test]
        public void Test_DeleteTime()
        {
            LocalSql.Instance.PunchIn();
            LocalSql.Instance.PunchOut();
            Assert.True(LocalSql.Instance.AllTimes().Count == 1);
            Time time = LocalSql.Instance.AllTimes().First();
            int result = LocalSql.Instance.DeleteTime(time.Key);
            Assert.True(result == 1);
            Assert.True(LocalSql.Instance.AllTimes().Count == 0);
        }

        [Test]
        public void Test_AddDay()
        {
            Assert.False(LocalSql.Instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
            LocalSql.Instance.AddDay(new Day(DateTime.Now.Date));
            Assert.True(LocalSql.Instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
        }

        [Test]
        public void Test_DeleteDay()
        {
            Assert.False(LocalSql.Instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
            LocalSql.Instance.AddDay(new Day(DateTime.Now.Date));
            Assert.True(LocalSql.Instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
            LocalSql.Instance.DeleteDay(DateTime.Now.Date);
            Assert.False(LocalSql.Instance.AllDays().Any(d => d.Date == DateTime.Now.Date));
        }

        [Test]
        public void Test_DeleteRange()
        {
            DateTime current = LocalSql.Instance.CurrentDay().Date;
            DateTime wayBack = new DateTime(2007, 6, 5);
            LocalSql.Instance.AddDay(new Day(wayBack));
            var days = LocalSql.Instance.AllDays();
            Assert.True(days.Any(d => d.Date == current));
            Assert.True(days.Any(d => d.Date == wayBack));
            LocalSql.Instance.DeleteRange(wayBack.AddDays(1), current);
            days = LocalSql.Instance.AllDays();
            foreach (Day day in days)
            {
                Assert.False(day.Date == current);
            }
            Assert.True(days.Any(d => d.Date == wayBack));
            LocalSql.Instance.DeleteRange(new DateTime(7, 6, 5), new DateTime(7, 6, 6));
            days = LocalSql.Instance.AllDays();
            Assert.False(days.Any(d => d.Date.Year == 2005 && d.Date.Month == 7 && d.Date.Day == 6));
        }

        [Test]
        public void Test_DeleteUser()
        {
            LocalSql.Instance.DeleteUser("nunit");
            Assert.True(LocalSql.Instance.UserNames.Count == 0);
        }

        [Test]
        public void Test_UpdateDetails()
        {
            LocalSql.Instance.AddDay(new Day(DateTime.Now.Date));
            LocalSql.Instance.UpdateDetails(DateTime.Now.Date, "Hello");
            Assert.True(LocalSql.Instance.AllDays().Last().Details == "Hello");
            LocalSql.Instance.UpdateDetails(DateTime.Now.Date, "GoodBy");
            Assert.True(LocalSql.Instance.AllDays().Last().Details == "GoodBy");
        }

        [Test]
        public void Test_UpdateTime()
        {
            LocalSql.Instance.PunchIn();
            LocalSql.Instance.PunchOut();
            DateTime now = DateTime.Now;
            Assert.True(LocalSql.Instance.UpdateTime(1, new Time(){TimeIn = now, TimeOut = DateTime.MaxValue}) == 1);
            Time last = LocalSql.Instance.AllTimes().Last();
            Assert.True(last.TimeIn.ToString() == now.ToString());
            Assert.True(last.TimeOut.ToString() == DateTime.MaxValue.ToString());
        }

        [Test]
        public void Test_MinDate()
        {
            LocalSql.Instance.AddDay(new Day(new DateTime(2009,4,25)));
            LocalSql.Instance.AddDay(new Day(new DateTime(2010,4,25)));
            LocalSql.Instance.AddDay(new Day(new DateTime(2011,4,25)));
            
            Assert.True(LocalSql.Instance.MinDate() == new DateTime(2009,4,25));
        }
        
        [Test]
        public void Test_MaxDate()
        {
            LocalSql.Instance.AddDay(new Day(new DateTime(2009,4,25)));
            LocalSql.Instance.AddDay(new Day(new DateTime(2010,4,25)));
            LocalSql.Instance.AddDay(new Day(new DateTime(2011,4,25)));
            
            Assert.True(LocalSql.Instance.MaxDate() == new DateTime(2011,4,25));
        }


        [Test]
        public void Test_PunchIn()
        {
            Assert.True(LocalSql.Instance.MaxTimeId() == 0);
            LocalSql.Instance.PunchIn();
            Time last = LocalSql.Instance.AllTimes().Last();
            Assert.True(last.TimeIn.ToString() == last.TimeOut.ToString());
            Assert.True(last.TimeIn.Millisecond == last.TimeOut.Millisecond);
            Assert.True(LocalSql.Instance.MaxTimeId() == 1);
        }

        [Test]
        public void Test_PunchOut()
        {
            Assert.True(LocalSql.Instance.MaxTimeId() == 0);
            LocalSql.Instance.PunchIn();
            Thread.Sleep(100);
            LocalSql.Instance.PunchOut();
            Time last = LocalSql.Instance.AllTimes().Last();
            Assert.True(last.TimeIn.Millisecond != last.TimeOut.Millisecond);
            
        }

        [Test]
        public void Test_StartEndWeek()
        {
            var cal = DateTimeFormatInfo.CurrentInfo.Calendar;
            Assert.True(TimeServer.StartEndWeek(DateTime.Now)[0].DayOfWeek == DayOfWeek.Sunday && TimeServer.StartEndWeek(DateTime.Now)[1].DayOfWeek == DayOfWeek.Saturday);
            Assert.True((TimeServer.StartEndWeek(DateTime.Now)[1].DayOfYear - TimeServer.StartEndWeek(DateTime.Now)[0].DayOfYear ) == 6);
        }
    }
}