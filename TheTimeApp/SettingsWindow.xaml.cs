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

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for w.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private TimeData.TimeData _data;
        
        public SettingsWindow(TimeData.TimeData data)
        {
            InitializeComponent();

            _data = data;

            TextBoxFromAddress.Text = AppSettings.FromAddress;
            TextBoxUserName.Text = AppSettings.FromUser;
            TextBoxFromPass.Text = "*********";
            TextBoxEmailServer.Text = AppSettings.EmailHost;
            TextBoxFromPort.Text = AppSettings.FromPort;
            TextBoxToAddress.Text = AppSettings.ToAddress;
            CheckBoxSsl.IsChecked = AppSettings.SslEmail == "true";

            TextBoxSqlDataSource.Text = AppSettings.SQLDataSource;
            TextBoxSqlUserId.Text = AppSettings.SQLUserId;
            TextBoxSqlPassword.Text = "*********";
            TextBoxSqlCatelog.Text = AppSettings.SQLCatelog;

            AssociateEvents();
        }

        private void AssociateEvents()
        {
            // email
            TextBoxFromAddress.TextChanged += TextBox_FromAddress_TextChanged;
            TextBoxUserName.TextChanged += TextBox_UserName_TextChanged;
            TextBoxFromPass.TextChanged += TextBox_FromPass_TextChanged;
            TextBoxEmailServer.TextChanged += TextBox_EmailServer_TextChanged;
            TextBoxFromPort.TextChanged += TextBox_FromPort_TextChanged;
            TextBoxToAddress.TextChanged += TextBox_ToAddress_TextChanged;

            // sql
            TextBoxSqlDataSource.TextChanged += TextBoxSqlDataSource_TextChanged;
            TextBoxSqlUserId.TextChanged += TextBoxSqlUserId_TextChanged;
            TextBoxSqlPassword.TextChanged += TextBoxSqlPassword_TextChanged;
            TextBoxSqlCatelog.TextChanged += TextBoxSqlCatelog_TextChanged;
        }

        private void TextBoxSqlDataSource_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SQLDataSource = TextBoxSqlDataSource.Text;
        }

        private void TextBoxSqlUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SQLUserId = TextBoxSqlUserId.Text;
        }

        private void TextBoxSqlPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SQLPassword = TextBoxSqlPassword.Text;
        }

        private void TextBoxSqlCatelog_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SQLCatelog = TextBoxSqlCatelog.Text;
        }

        private void TextBox_FromAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.FromAddress = TextBoxFromAddress.Text;
        }

        private void TextBox_UserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.FromUser = TextBoxUserName.Text;
        }

        private void TextBox_FromPass_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.FromPass = TextBoxFromPass.Text;
        }

        private void TextBox_EmailServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.EmailHost = TextBoxEmailServer.Text;
        }

        private void TextBox_FromPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.FromPort = TextBoxFromPort.Text;
        }

        private void TextBox_ToAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.ToAddress = TextBoxToAddress.Text;
        }

        private void Ssl_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.SslEmail = "true";
        }

        private void Ssl_CheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            AppSettings.SslEmail = "false";
        }
    }
}
