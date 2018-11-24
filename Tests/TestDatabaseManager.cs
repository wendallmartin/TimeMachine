using System;
using System.Collections.Generic;
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
    public class TestDatabaseManager
    {
        private TimeServer _instance;
        [SetUp]
        public void Setup()
        {
            TimeServer.SqlCurrentUser = "nunit";
            
            AppSettings.Instance = new AppSettings();
            AppSettings.Instance.DataPath = "TimeData.sqlite";
            AppSettings.Instance.MySqlDatabase = "test";
            AppSettings.Instance.MySqlPassword = "test";
            AppSettings.Instance.MySqlPort = 3306;
            AppSettings.Instance.MySqlServer = "localhost";
            AppSettings.Instance.MySqlUserId = "WendallMartin";

            DataBaseManager.Initualize();
            _instance = DataBaseManager.Instance;
            _instance.AddUser(new User("nunit", "", new List<Day>()));
            TimeServer.SqlCurrentUser = "nunit";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (string name in _instance.UserNames())
            {
                _instance.DeleteUser(name);
            }
            _instance.Dispose();
            
            Thread.Sleep(100);// Give sqlite time to dispose.
            
            if(File.Exists(AppSettings.Instance.DataPath))
                File.Delete(AppSettings.Instance.DataPath);
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
        public void TestGitCommit()
        {
            DateTime now = DateTime.Now;
            DateTime wayBack = new DateTime(2000, 2, 5);
            
            GitCommit commitA = new GitCommit("testA", now, "commit A", Guid.NewGuid().ToString())
            {
                Branch = "commitABranch",
                Url = "wrmcodeblocks.com"
            };
            
            GitCommit commitB = new GitCommit("testB", now, "commit B", Guid.NewGuid().ToString())
            {
                Branch = "commitBBranch",
                Url = "urlB"
            };
            
            GitCommit commitC = new GitCommit("testC", now, "commit C", Guid.NewGuid().ToString());
            
            GitCommit commitD = new GitCommit("testD", wayBack, "work back here?", Guid.NewGuid().ToString());
            
            _instance.AddCommit(commitA);
            _instance.AddCommit(commitA);
            _instance.AddCommit(commitA);
            
            Assert.IsTrue(_instance.GetCommits(now).Count == 1);// Test duplicate safety feature.
            
            _instance.AddCommit(commitB);
            
            Assert.IsTrue(_instance.GetCommits(now).Count == 2);
            
            _instance.AddCommit(commitC);
            
            _instance.AddCommit(commitD);
            
            Assert.IsTrue(_instance.GetCommits(now).Count == 3);

            var commits = _instance.GetCommits(now);

            Assert.IsTrue(commits.Count == 3);
            
            Assert.IsTrue(commitA.Equals(commits[0]));
            
            Assert.IsTrue(commitB.Equals(commits[1]));
            
            Assert.IsTrue(commitC.Equals(commits[2]));
            
            _instance.RemoveCommit(commitA);
            
            Assert.IsTrue(_instance.GetCommits(now).Count == 2);
            
            _instance.RemoveCommit(commitB);
            
            Assert.IsTrue(_instance.GetCommits(now).Count == 1);
            
            _instance.RemoveCommit(commitC);
            
            Assert.IsTrue(_instance.GetCommits(now).Count == 0);
            
            Assert.IsTrue(_instance.GetCommits().Count == 1);
        }
    }
}