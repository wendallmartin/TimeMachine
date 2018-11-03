using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TheTimeApp.TimeData
{
    [Serializable]
    public class Day
    {
        private string _details = "";
        private List<Time> times;
        private DateTime _date;
        internal bool Emailed;

        /// <summary>
        /// The user: Add with multiple user feature.
        /// </summary>
        [OptionalField] private string _user;

        public Day(DateTime date)
        {
            _date = date.Date;
            _details = "";
            times = new List<Time>();
        }

        public DateTime Date
        {
            get{ return _date.Date; }
            set{ _date = value; }
        }

        public void AddTime(Time time)
        {
            times.Add(time);
        }

        public string User
        {
            get => _user;
            set => _user = value;
        }

        public void ClearTimes()
        {
            times.Clear();
        }

        public List<Time> Times
        {
            get{ return times; } 
            set{ times = value; } 
        }

        public string Details
        {
            get{ return _details == null ? String.Empty : _details; }
            set { _details = value; } 
        }

        public bool Contains(Time time)
        {
            foreach (Time t in times)
            {
                if (t == time) return true;
            }
            return false;
        }

        public bool HasTime()
        {
            return times.Count > 0;
        }

        public double HoursAsDec => Hours().TotalHours;

        public double HoursAsDecToQuarter => TimeServer.DecToQuarter(HoursAsDec);

        public TimeSpan Hours()
        {
            TimeSpan total = new TimeSpan();
            foreach (Time time in times)
            {
                total += time.GetTime();
            }

            return total;
        }

        public List<Time> GetTimes()
        {
            return times;
        }

        public static bool Equals(Day a, Day b)
        {
            if (a.times.Count != b.times.Count) return false;
            for (int i = 0; i < a.times.Count; i++)
            {
                if (a.times[i].TimeIn.ToString() != b.times[i].TimeIn.ToString()) return false;
                if (a.times[i].TimeOut.ToString() != b.times[i].TimeOut.ToString()) return false;
                if (a.times[i].Key != b.times[i].Key) return false;
            }

            if (a._date.ToString() != b._date.ToString()) return false;
            if (a._details != b._details) return false;
            if (a.Emailed.ToString() != b.Emailed.ToString()) return false;

            return true;
        }
    }
}
