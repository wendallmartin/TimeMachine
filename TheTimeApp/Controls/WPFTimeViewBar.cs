using System.Windows.Forms;
using System.Windows.Media;
using TheTimeApp.TimeData;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TheTimeApp.Controls
{
    /// <inheritdoc />
    /// <summary>
    /// The time bar. Inherites ViewBar.
    /// </summary>
    public class WpfTimeViewBar : ViewBar 
    {
        private readonly Time _time;

        public delegate void TimeDeleteDel(Time time);

        public delegate void TimeSelectedDel(WpfTimeViewBar viewbar);
        
        public event TimeDeleteDel TimeDeleteEvent;

        public event TimeSelectedDel TimeClickEvent;

        public WpfTimeViewBar(Time time, bool is25Hour)
        {
            BrushSelected = Brushes.LightCyan;
            BrushUnselected = Brushes.Beige;
         
            _time = time;

            if (is25Hour)
            {
                Text = "        In: " + _time.TimeIn.ToString("HH:mm") + "   Out: " + _time.TimeOut.ToString("HH:mm")
                       + "   Total: " + _time.GetTime().Hours + ":" + _time.GetTime().Minutes;   
            }
            else
            {
                Text = "        In: " + _time.TimeIn.ToString("hh:mm tt") + "   Out: " + _time.TimeOut.ToString("hh:mm tt")
                       + "   Total: " + _time.GetTime().Hours + ":" + _time.GetTime().Minutes;
            }

            SelectedEvent += OnMouseClick;
            DeleteEvent += OnDeleteClick;
            
        }

        private void OnDeleteClick(ViewBar viewBar)
        {
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            var sure = MessageBox.Show(@"Time will be deleted permenetly!", @"Warning", buttons);

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
