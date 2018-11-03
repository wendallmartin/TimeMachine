using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using static TheTimeApp.TimeData.TimeServer;
using TheTimeApp.TimeData;

namespace Tests
{
    public class TestTimeServer
    {
//        [Test]
        public void TestDaysToHtml()
        {
            Sqlite sqlite = Sqlite.LoadFromFile();
            SqlCurrentUser = "Wendall";
            List<Day> days = sqlite.DaysInRange(StartEndWeek(DateTime.Now)[0], StartEndWeek(DateTime.Now)[1]);
            File.WriteAllText(@"C:\Users\Wendall Martin\Desktop\TimeReport.html", HtmlTimeReporter.DaysToHtml(days));
        }

        [Test]
        public void TestDecToQuarter()
        {
            Assert.IsTrue(DecToQuarter(9.875) == 10);
            Assert.IsTrue(DecToQuarter(6.824) == 6.75);
            Assert.IsTrue(DecToQuarter(9.626) == 9.75);
            Assert.IsTrue(DecToQuarter(3.623) == 3.5);
            Assert.IsTrue(DecToQuarter(2.380) == 2.5);
            Assert.IsTrue(DecToQuarter(15.370) == 15.25);
            Assert.IsTrue(DecToQuarter(20.126) == 20.25);
            Assert.IsTrue(DecToQuarter(7.124) == 7);
        }
    }
}