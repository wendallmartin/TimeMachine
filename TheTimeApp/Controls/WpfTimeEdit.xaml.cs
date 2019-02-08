using System.Windows;
using TheTimeApp.TimeData;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for WpfTimeEdit.xaml
    /// </summary>
    public partial class WpfTimeEdit
    {
        public Time Time { get; }
        
        public WpfTimeEdit(Time time)
        {
            InitializeComponent();

            Time = time;

            InTime.DateTime = time.TimeIn;
            OutTime.DateTime = time.TimeOut;
        }

        private void Btn_SaveClick(object sender, RoutedEventArgs e)
        {
            Time.TimeIn = InTime.DateTime;
            Time.TimeOut = OutTime.DateTime;

            DialogResult = true;
            Close();
        }
    }
}
