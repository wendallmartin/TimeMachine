using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TheTimeApp.TimeData;
using Color = System.Drawing.Color;
using MessageBox = System.Windows.Forms.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for WPFTimeViewBar.xaml
    /// </summary>
    public partial class WPFTimeViewBar
    {
        private Time _time;

        TimeViewEdit timeedit;

        public delegate void DeleteDel(Time time);

        public event TimeView.DeleteDel DeleteEvent;

        public delegate void SelectedDel(WPFTimeViewBar viewbar);

        public event SelectedDel SelectedEvent;

        private bool _is12hour;

        public bool Editable { get; set; }

        public WPFTimeViewBar()
        {
            InitializeComponent();

            if (_is12hour)
            {
                label.Content = "        In: " + _time.TimeIn.ToString("hh:mm tt") + "   Out: " + _time.TimeOut.ToString("hh:mm tt")
                                 + "   Total: " + _time.GetTime().Hours + ":" + _time.GetTime().Minutes;
            }
            else
            {
                label.Content = "        In: " + _time.TimeIn.ToString("HH:mm") + "   Out: " + _time.TimeOut.ToString("HH:mm")
                                 + "   Total: " + _time.GetTime().Hours + ":" + _time.GetTime().Minutes;
            }
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            var sure = MessageBox.Show("Time will be deleted permenetly!", "Warning", buttons);

            if (sure == DialogResult.Yes)
            {
                DeleteEvent?.Invoke(_time);
            }
        }

        private void OnMouseClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            SelectedEvent?.Invoke(this);
        }

        public Time GetTime()
        {
            return _time;
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            if (Editable)
                delete.Visibility = Visibility.Visible;

            Background = Brushes.CornflowerBlue;
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (!IsMouseOver)
            {
                if (Editable)
                    delete.Visibility = Visibility.Hidden;
                Background = Brushes.LightSlateGray;
            }
        }

        public void UnSelect()
        {
            if (Editable)
                delete.Visibility = Visibility.Hidden;
            Background = Brushes.LightSlateGray;
        }
    }
}
