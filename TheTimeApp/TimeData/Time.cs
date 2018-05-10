using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TheTimeApp.TimeData
{
    [Serializable]
    public class Time
    {
        private DateTime punchin;
        private DateTime punchout;

        public Time()
        {
            punchin = new DateTime();
            punchout = new DateTime();
        }

        public void PunchIn()
        {
            if (punchin != new DateTime())
            {
                MessageBox.Show("Punch in failed!");
                return;
            }

            punchin = DateTime.Now;
        }

        public void PunchOut()
        {
            if (punchout != new DateTime())
            {
                MessageBox.Show("Punch out failed!");
                return;
            }
            punchout = DateTime.Now;
        }

        public DateTime TimeIn
        {
            get{ return punchin;}
            set{ punchin = value; }
        }

        public DateTime TimeOut
        {
            get{ return punchout;}
            set{ punchout = value; }
        }

        public TimeSpan GetTime()
        {
            return punchout - punchin;
        }
    }
}
