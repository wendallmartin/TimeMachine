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

            DetailsCommitView.Date = day.Date;
            DetailsCommitView.DayDetails = day.Details;
        }

        private void OnDayDetailsChanged(string details)
        {
            if (!Enabled) return;
                
            DataBaseManager.Instance.UpdateDetails(_day.Date, details);
        }
    }
}
