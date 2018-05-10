using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TheTimeApp.TimeData;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for wpfDayViewEdit.xaml
    /// </summary>
    public partial class wpfDayViewEdit : Window
    {
        private Day _day;
        public wpfDayViewEdit(Day day)
        {
            _day = day;
            InitializeComponent();
            dayDetails.Text = day.Details;
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _day.Details = dayDetails.Text;
        }
    }
}
