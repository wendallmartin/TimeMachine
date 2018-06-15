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
using Button = System.Windows.Controls.Button;
using Color = System.Drawing.Color;
using Control = System.Windows.Controls.Control;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.Forms.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for WPFWeekViewBar.xaml
    /// </summary>
    public class WPFWeekViewBar : ViewBar 
    {
        private Label seperaterLabel;
        private DateTime date;

        public delegate void WeekDel(DateTime date);

        public event WeekDel DeleteWeekEvent;

        public event WeekDel EmailWeekEvent;

        public event WeekDel PrintWeekEvent;

        public event WeekDel PreviewWeekEvent;

        public WPFWeekViewBar(DateTime dateTime, double hoursinweek)
        {
            BrushSelected = Brushes.DimGray;
            BrushUnselected = Brushes.Gray;
            
            date = dateTime;

            Text = "Week - " + dateTime.Month + "//" +
                   dateTime.Day + "//" + dateTime.Year + "                                                         Hours: " + hoursinweek;

            DeleteEvent += OnDeleteDay;
            SelectedEvent += OnMouseDown;
        }

        private void OnDeleteDay(ViewBar viewBar)
        {
            var sure = MessageBox.Show(@"Time will be deleted permenetly!", @"Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (sure == DialogResult.Yes)
            {
                DeleteWeekEvent?.Invoke(date);
            }
        }

        private void OnMouseDown()
        {
            EmailOrPrintWindow emailOrPrintWindow = new EmailOrPrintWindow();

            emailOrPrintWindow.ShowDialog();

            var sure = emailOrPrintWindow.DialogResult;

            if (sure == DialogResult.Yes)
            {
                EmailWeekEvent?.Invoke(date);
            }
            else if (sure == DialogResult.No)
            {
                PrintWeekEvent?.Invoke(date);
            }
            else if (sure == DialogResult.OK)
            {
                PreviewWeekEvent?.Invoke(date);
            }
        }
    }
}
