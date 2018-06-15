using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for WPFDayViewBar.xaml
    /// </summary>
    public partial class WPFDayViewBar : ViewBar
    {
        private DateTime date;

        public delegate void DayDelegate(DateTime date);

        public event DayDelegate DeleteDayEvent;

        public event DayDelegate DayClickEvent;

        public bool Editable { get; set; }

        public WPFDayViewBar(TimeData.Day day)
        {
            BrushSelected = Brushes.LightSkyBlue;
            BrushUnselected = Brushes.CadetBlue;
            
            date = day.Date;
            
            Text = date.Month + "//" + date.Day + "//" + date.Year + "                                              Hours: " + day.HoursAsDec();

            DeleteEvent += OnDeleteDay;
            SelectedEvent += OnDayDayClick;
        }

        private void OnDayDayClick()
        {
            DayClickEvent?.Invoke(date);
        }

        private void OnDeleteDay(ViewBar viewBar)
        {
            MessageBoxButton button = MessageBoxButton.YesNo;
            var sure = MessageBox.Show("Time will be deleted permenetly!", "Warning", button);

            if (sure == (MessageBoxResult) DialogResult.Yes)
            {
                DeleteDayEvent?.Invoke(date);
            }
        }
    }
}
