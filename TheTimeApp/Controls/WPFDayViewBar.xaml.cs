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
    public partial class WPFDayViewBar : UserControl
    {
        private DateTime date;

        public delegate void DayDelegate(DateTime date);

        public event DayDelegate DeleteDayEvent;

        public event DayDelegate SelectedEvent;

        public bool Editable { get; set; }

        public WPFDayViewBar(Size size, TimeData.Day day)
        {
            InitializeComponent();

            Width = size.Width;
            Height = size.Height;
            
            date = day.Date;
            
            label.Content = date.Month + "//" + date.Day + "//" + date.Year + "                                              Hours: " + day.HoursAsDec();
        }

        private void OnDayClick(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            SelectedEvent?.Invoke(date);
        }

        private void OnDeleteDay(object sender, MouseEventArgs e)
        {
            MessageBoxButton button = MessageBoxButton.YesNo;
            var sure = MessageBox.Show("Time will be deleted permenetly!", "Warning", button);

            if (sure == (MessageBoxResult) DialogResult.Yes)
            {
                DeleteDayEvent?.Invoke(date);
            }
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (!IsMouseOver && !delete.IsMouseOver)
            {
                Background = Brushes.Green;
                if (Editable)
                    delete.Visibility = Visibility.Hidden;
            }
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            Background = Brushes.LightGreen;
            if (Editable)
                delete.Visibility = Visibility.Visible;
        }

        public DateTime Date
        {
            get { return date; }
        }
    }
}
