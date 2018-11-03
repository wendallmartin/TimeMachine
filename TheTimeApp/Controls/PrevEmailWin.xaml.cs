using System.Windows;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for PrevEmailWin.xaml
    /// Returns true if email.
    /// Returns false if preview.
    /// </summary>
    public partial class PrevEmailWin
    {
        public enum ResultValue
        {
            Email,
            Prev,
            Cancel
        }

        public ResultValue Result { get; private set; }
        
        public PrevEmailWin()
        {
            InitializeComponent();
            Result = ResultValue.Cancel;
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultValue.Prev;
            Close();
        }

        private void EmailBtn_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultValue.Email;
            Close();
        }
    }
}
