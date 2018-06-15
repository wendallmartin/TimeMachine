using System.Windows.Forms;
using System.Windows.Media;
using TheTimeApp.TimeData;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for WPFTimeViewBar.xaml
    /// </summary>
    public class WPFTimeViewBar : ViewBar 
    {
        private Time _time;

        TimeViewEdit timeedit;

        public delegate void TimeDeleteDel(Time time);

        public delegate void TimeSelectedDel(WPFTimeViewBar viewbar);
        
        public event TimeDeleteDel TimeDeleteEvent;

        public event TimeSelectedDel TimeClickEvent;

        private bool _is12hour;

        public bool Editable { get; set; }

        public WPFTimeViewBar(Time time, bool is12hour)
        {
            BrushSelected = Brushes.LightCyan;
            BrushUnselected = Brushes.Beige;
         
            _time = time;

            _is12hour = is12hour;

            if (_is12hour)
            {
                Text = "        In: " + _time.TimeIn.ToString("hh:mm tt") + "   Out: " + _time.TimeOut.ToString("hh:mm tt")
                                 + "   Total: " + _time.GetTime().Hours + ":" + _time.GetTime().Minutes;
            }
            else
            {
                Text = "        In: " + _time.TimeIn.ToString("HH:mm") + "   Out: " + _time.TimeOut.ToString("HH:mm")
                                 + "   Total: " + _time.GetTime().Hours + ":" + _time.GetTime().Minutes;
            }

            SelectedEvent += OnMouseClick;
            DeleteEvent += OnDeleteClick;
            
        }

        private void OnDeleteClick(ViewBar viewBar)
        {
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            var sure = MessageBox.Show("Time will be deleted permenetly!", "Warning", buttons);

            if (sure == DialogResult.Yes)
            {
                TimeDeleteEvent?.Invoke(_time);
            }
        }

        private void OnMouseClick()
        {
            TimeClickEvent?.Invoke(this);
        }

        public Time GetTime()
        {
            return _time;
        }
    }
}
