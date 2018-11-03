using System;
using System.Globalization;
using System.Windows.Forms;

namespace TheTimeApp.TimeData
{
    [Serializable]
    public class Time
    {
        private DateTime punchin;
        private DateTime punchout;
        public string Key;

        public Time(DateTime punchIn, DateTime punchOUt)
        {
            punchin = punchIn;
            punchout = punchOUt;
        }
        
        public Time()
        {
            punchin = new DateTime();
            punchout = new DateTime();
        }
        
        public void PunchIn()
        {
            if (punchin != new DateTime())
            {
                throw new Exception(@"Punch in failed!");
            }

            punchin = punchout = DateTime.Now;
        }

        public void PunchOut()
        {
            punchout = DateTime.Now;
        }

        public DateTime TimeIn
        {
            get => punchin;
            set => punchin = value;
        }

        public DateTime TimeOut
        {
            get => punchout;
            set => punchout = value;
        }

        public TimeSpan GetTime()
        {
            return punchout - punchin;
        }

        /// <summary>
        /// Since both timin and timeout are
        /// set at punchin, if the equal eachouther we
        /// are still pucnhed in.
        /// </summary>
        /// <returns></returns>
        public bool IsPunchedIn()
        {
            return TimeIn.TimeOfDay.ToString() == TimeOut.TimeOfDay.ToString();
        }
    }
}
