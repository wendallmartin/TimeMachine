using System.Windows.Controls;
using TheTimeApp.TimeData;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for WpfDayViewEdit.xaml
    /// </summary>
    public partial class WpfDayViewEdit
    {
        private Day _day;

        public bool Enabled { get; set; } = true;
        
        public WpfDayViewEdit(Day day)
        {
            _day = day;
            InitializeComponent();
            dayDetails.Text = day.Details;
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Enabled) return;
                
            DataBaseManager.Instance.UpdateDetails(_day.Date, dayDetails.Text);
        }
    }
}
