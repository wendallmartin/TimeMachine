using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Win32;
using TheTimeApp.TimeData;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for w.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private TimeData.TimeData _data;
        private SqlServerHelper _sqlHelper = new SqlServerHelper(TimeData.TimeData.Commands);
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
            TextBoxSqlPort.Text = AppSettings.SQLPortNumber;
            
            btn_SQLEnable.Content = AppSettings.SQLEnabled == "true" ? "Enabled" : "Disabled";
            btn_Permission.Content = AppSettings.MainPermission == "write" ? "Write" : "Read";

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
            TextBoxSqlPort.TextChanged += TextBoxSqlPort_TextChanged;

            // sql server progress changed
            _sqlHelper.ProgressChangedEvent += OnSqlProgressChanged;
            _sqlHelper.ProgressFinishEvent += OnProgressFinish;
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

        private void Btn_SQLEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.SQLEnabled == "true")
            {
                AppSettings.SQLEnabled = "false";
                btn_SQLEnable.Content = "Disabled";
            }
            else
            {
                AppSettings.SQLEnabled = "true";
                btn_SQLEnable.Content = "Enabled";
            }
        }
        
        private void Btn_SQLPushAll_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar_SQLRePushAll.Visibility = Visibility.Visible;
            btn_SQLSyncAll.IsEnabled = false;
            _sqlHelper.RePushToServer(_data.Days);
        }

        private void OnSqlProgressChanged(float value)
        {
            Debug.WriteLine(value);
            Dispatcher.Invoke(new Action(() =>
            {
                ProgressBar_SQLRePushAll.Value = value;
                
            
                if (value == 100)
                {
                    ProgressBar_SQLRePushAll.Visibility = Visibility.Hidden;
                }
            }));
        }
        
        private void OnProgressFinish()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                btn_SQLSyncAll.IsEnabled = true;
                ProgressBar_SQLRePushAll.Value = 0;
                ProgressBar_SQLRePushAll.Visibility = Visibility.Hidden;
            }));
        }


        private void btn_EmployEmployer_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.MainPermission == "write")
            {
                AppSettings.MainPermission = "read";
                btn_Permission.Content = "Read";
            }
            else
            {
                AppSettings.MainPermission = "write";
                btn_Permission.Content = "Write";
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender as TabItem == Developer_Settings)
            {
                MessageBox.Show("Enter Password");
            }
        }

        private void TextBoxSqlPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SQLPortNumber = (sender as TextBox)?.Text;
        }

        private void Btn_SQLDownload_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Time file (*.dtf)|*.dtf";
            saveFileDialog.ShowDialog();
            ProgressBar_SQLRePushAll.Visibility = Visibility.Visible;
            TimeData.TimeData.LoadDataFromSqlSever().Save();
        }

        private void DataLocation_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Time file (*.dtf)|*.dtf";
            openFileDialog.ShowDialog();
            AppSettings.DataPath = openFileDialog.FileName;
        }
    }
}
