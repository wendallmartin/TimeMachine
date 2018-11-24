using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TheTimeApp.TimeData;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for DetailsCommitView.xaml
    /// </summary>
    public partial class DetailsCommitView
    {
        public delegate void DetailsChangedDel(string newDetails);

        public event DetailsChangedDel DetailsChangedEvent;

        public string DayDetails
        {
            get => DayDetailsBox.Text;
            set => DayDetailsBox.Text = value;
        }

        private DateTime _date;

        public DateTime Date { get => _date;
            set {
                _date = value;
                LoadCommitMsgs();
            }
        }

        private bool _gitCommits;

        public DetailsCommitView()
        {
            InitializeComponent();

            Init();
        }

        public void Init()
        {
            BtnDetailsCommits.Visibility = AppSettings.Instance.GitEnabled ? Visibility.Visible : Visibility.Hidden;
        }

        private void DayDetailsBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DetailsChangedEvent?.Invoke(DayDetailsBox.Text);
        }
        
        private void GitCommitsBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCommitMsgs();
        }

        private void btn_DetailsCommits_Click(object sender, RoutedEventArgs e)
        {
            ToggleDetailsCommits();
        }
        
        private void ToggleDetailsCommits()
        {
            _gitCommits = !_gitCommits;
            if (_gitCommits)
            {
                LoadCommitMsgs();

                Label.Content = "Commits";
                Label.Visibility = Visibility.Visible;
                Task.Delay(2000).ContinueWith(e => Dispatcher.Invoke(() => Label.Visibility = Visibility.Hidden));
                
                DayDetailsBox.Visibility = Visibility.Hidden;
                GitCommitsBox.Visibility = Visibility.Visible;
                
                ImageDetails.Visibility = Visibility.Hidden;
                ImageCommits.Visibility = Visibility.Visible;
            }
            else
            {
                Label.Content = "Details";
                Label.Visibility = Visibility.Visible;
                Task.Delay(2000).ContinueWith(e => Dispatcher.Invoke(() => Label.Visibility = Visibility.Hidden));
                
                GitCommitsBox.Visibility = Visibility.Hidden;
                DayDetailsBox.Visibility = Visibility.Visible;
                
                ImageCommits.Visibility = Visibility.Hidden;
                ImageDetails.Visibility = Visibility.Visible;
            }
        }

        public void LoadCommitMsgs()
        {
            if (!AppSettings.Instance.GitEnabled) return;
            if (!CheckAccess())
            {
                Dispatcher.Invoke(LoadCommitMsgs);
                return;
            }

            GitCommitsBox.TextChanged -= GitCommitsBox_OnTextChanged;

            GitCommitsBox.Text = "";
            foreach (GitCommit commit in DataBaseManager.Instance.GetCommits(_date.Date))
            {
                GitCommitsBox.Text += "-o- " +  commit.Message;
            }
            
            GitCommitsBox.TextChanged += GitCommitsBox_OnTextChanged;
        }
    }
}
