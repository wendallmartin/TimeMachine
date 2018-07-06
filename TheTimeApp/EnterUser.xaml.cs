using System.Windows;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for EnterUser.xaml
    /// </summary>
    public partial class EnterUser
    {
        public EnterUser()
        {
            InitializeComponent();
        }

        public string UserText => txt_User.Text;

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
