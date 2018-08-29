using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using Day = TheTimeApp.TimeData.Day;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for w.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private readonly object _update = new object();
        
        public SettingsWindow()
        {
            InitializeComponent();

            TextBoxFromAddress.Text = AppSettings.FromAddress;
            TextBoxUserName.Text = AppSettings.FromUser;
            TextBoxFromPass.Text = "*********";
            TextBoxEmailServer.Text = AppSettings.EmailHost;
            TextBoxFromPort.Text = AppSettings.FromPort;
            TextBoxToAddress.Text = AppSettings.ToAddress;
            CheckBoxSsl.IsChecked = AppSettings.SslEmail == "true";

            TextBoxAzureDataSource.Text = AppSettings.AzureDateSource;
            TextBoxAzureUserId.Text = AppSettings.AzureUser;
            TextBoxAzurePassword.Text = "*********";
            TextBoxAzureCatelog.Text = AppSettings.AzureCateloge;
            TextBoxAzurePort.Text = AppSettings.AzurePort;
            
            TextBoxMySqlDataSource.Text = AppSettings.MySqlServer;
            TextBoxMySqlUserId.Text = AppSettings.MySqlUserId;
            TextBoxMySqlPassword.Text = "*********";
            TextBoxMySqlPort.Text = AppSettings.MySqlPort.ToString();
            TextBoxMySqlDatabase.Text = AppSettings.MySqlDatabase;
            
            btn_SQLEnable.Content = AppSettings.SqlEnabled == "true" ? "Enabled" : "Disabled";
            btn_Permission.Content = AppSettings.MainPermission == "write" ? "Write" : "Read";

            SqlTypeExpaner.Header = AppSettings.SqlType;

            switch (AppSettings.SqlType)
            {
                    case "Azure":
                        MySqlSettings.Visibility = Visibility.Hidden;
                        AzureSettings.Visibility = Visibility.Visible;
                        break;
                    case "MySql":
                        AzureSettings.Visibility = Visibility.Hidden;
                        MySqlSettings.Visibility = Visibility.Visible;
                        break;
                    default:
                        AppSettings.SqlType = "Azure";
                        MySqlSettings.Visibility = Visibility.Hidden;
                        AzureSettings.Visibility = Visibility.Visible;
                        return;
            }

            AssociateEvents();
            LoadUsers();
        }

        private void LoadUsers()
        {
            StackPanel.Children.Clear();
            foreach (string user in DataBaseManager.Instance.UserNames())
            {
                ViewBar userbar = new ViewBar()
                {
                    BrushUnselected = Brushes.DarkGray,
                    BrushSelected = Brushes.DimGray,
                    Text = user, 
                    Width = 220, 
                    Height = 25, 
                    Deletable = true
                };
                userbar.DeleteEvent += OnDeleteUser;
                StackPanel.Children.Add(userbar);
            }
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

            // azure
            TextBoxAzureDataSource.TextChanged += TextBoxAzureDataSource_TextChanged;
            TextBoxAzureUserId.TextChanged += TextBoxAzureUserId_TextChanged;
            TextBoxAzurePassword.TextChanged += TextBoxAzurePassword_TextChanged;
            TextBoxAzureCatelog.TextChanged += TextBoxAzureCatelog_TextChanged;
            TextBoxAzurePort.TextChanged += TextBoxAzurePort_TextChanged;
            
            // mysql
            TextBoxMySqlDataSource.TextChanged += TextBoxMySqlDataSource_TextChanged;
            TextBoxMySqlUserId.TextChanged += TextBoxMySqlUserId_TextChanged;
            TextBoxMySqlPassword.TextChanged += TextBoxMySqlPassword_TextChanged;
            TextBoxMySqlPort.TextChanged += TextBoxMySqlPort_TextChanged;
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
            if (AppSettings.SqlEnabled == "true")
            {
                AppSettings.SqlEnabled = "false";
                btn_SQLEnable.Content = "Disabled";
            }
            else
            {
                AppSettings.SqlEnabled = "true";
                btn_SQLEnable.Content = "Enabled";
            }
        }
        
        private void Btn_SQLPushAll_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.SqlEnabled != "true")
            {
                MessageBox.Show("SQL not enabled!");
                return;
            }
            
            ProgressBar_SQLRePushAll.Visibility = Visibility.Visible;
            btn_SQLSyncAll.IsEnabled = false;
            DataBaseManager.Instance.RePushToServer();
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

        private void btn_Add_Click(object sender, RoutedEventArgs e)
        {
            DataBaseManager.Instance.AddUser(new User(AddUserBox.Text, "", new List<Day>()));
            
            AddUserBox.Text = "";
            LoadUsers();
        }

        private void OnDeleteUser(ViewBar viewbar)
        {
            DataBaseManager.Instance.DeleteUser(viewbar.Text);
            LoadUsers();
        }

        private void Btn_CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                lock (_update)
                {
                    try
                    {
                        ProcessStartInfo info = new ProcessStartInfo();
                        info.FileName = Path.Combine(Directory.GetCurrentDirectory(), "FTPUpdater.exe");
                        info.Arguments = Program.CurrentVersion + $" \"{Directory.GetCurrentDirectory()}\"" + " TheTimeApp";
                        Process.Start(info);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show($"Cannot update! {exception.Message}.");
                    }
                }
            }).Start();
        }

        #region sql settings
        
        private void Btn_SQLDownload_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.SqlEnabled != "true")
            {
                MessageBox.Show("SQL not enabled!");
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Time file (*.sqlite)|*.sqlite";
            saveFileDialog.ShowDialog();
            ProgressBar_SQLRePushAll.Visibility = Visibility.Visible;
            DataBaseManager.Instance.LoadFromServer();
        }

        private void DataLocation_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Time file (*.sqlite)|*.sqlite";
            openFileDialog.ShowDialog();
            AppSettings.DataPath = openFileDialog.FileName;
        }

        private void BtnTestClick(object sender, RoutedEventArgs e)
        {

            try
            {
                switch (AppSettings.SqlType)
                {
                    case "Azure":
                        SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder()
                        {
                            DataSource = AppSettings.AzureDateSource,
                            UserID = AppSettings.AzureUser,
                            Password = AppSettings.AzurePassword,
                            InitialCatalog = AppSettings.AzureCateloge,
                            MultipleActiveResultSets = true,
                        };
                        using (SqlConnection azureConnection = new SqlConnection(stringBuilder.ConnectionString))
                        {
                            azureConnection.Open();
                            azureConnection.Close();
                        }

                        MessageBox.Show("Connection successful!");
                        break;
                    case "MySql":
                        MySqlConnectionStringBuilder mysqlBuiler = new MySqlConnectionStringBuilder()
                        {
                            Server = AppSettings.MySqlServer,
                            UserID = AppSettings.MySqlUserId,
                            Password = AppSettings.MySqlPassword,
                            Port = (uint) AppSettings.MySqlPort,
                            SslMode = MySqlSslMode.None// todo add mysql ssl setting
                        };
                        using (MySqlConnection mySqlConnection = new MySqlConnection(mysqlBuiler.ConnectionString))
                        {
                            mySqlConnection.Open();
                            mySqlConnection.Close();
                        }
                        MessageBox.Show("Connection successful!");
                        break;
                    default:
                        MessageBox.Show("Sql type not recognized!");
                        break;
                }

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void BtnTypeAzure_Click(object sender, RoutedEventArgs e)
        {
            SqlTypeExpaner.Header = AppSettings.SqlType = "Azure";
            MySqlSettings.Visibility = Visibility.Hidden;
            AzureSettings.Visibility = Visibility.Visible;
            SqlTypeExpaner.IsExpanded = false;
        }

        private void BtnTypeMySql_Click(object sender, RoutedEventArgs e)
        {
            SqlTypeExpaner.Header = AppSettings.SqlType = "MySql";
            AzureSettings.Visibility = Visibility.Hidden;
            MySqlSettings.Visibility = Visibility.Visible;
            SqlTypeExpaner.IsExpanded = false;
        }

        #region azure value change

        private void TextBoxAzureDataSource_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.AzureDateSource = TextBoxAzureDataSource.Text;
        }

        private void TextBoxAzureUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.AzureUser = TextBoxAzureUserId.Text;
        }

        private void TextBoxAzurePassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.AzurePassword = TextBoxAzurePassword.Text;
        }

        private void TextBoxAzureCatelog_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.AzureCateloge = TextBoxAzureCatelog.Text;
        }
        
        private void TextBoxAzurePort_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.AzurePort = (sender as TextBox)?.Text;
        }
        
        private void TextBoxMySqlPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.MySqlPort = int.Parse(TextBoxMySqlPort.Text);
        }

        private void TextBoxMySqlPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.MySqlPassword = TextBoxMySqlPassword.Text;
        }

        private void TextBoxMySqlUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.MySqlUserId = TextBoxMySqlUserId.Text;
        }

        private void TextBoxMySqlDataSource_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.MySqlServer = TextBoxMySqlDataSource.Text;
        }
        
        private void TextBoxMySqlDatabase_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.MySqlDatabase = TextBoxMySqlDatabase.Text;
        }

        #endregion

        #endregion

        private void SqlTypeExpaner_Expanded(object sender, RoutedEventArgs e)
        {
            btn_SQLSyncAll.Visibility = btn_SQLBackup.Visibility = btn_SQLEnable.Visibility = btn_SQLTest.Visibility = Visibility.Hidden;
        }

        private void SqlTypeExpaner_Colapsed(object sender, RoutedEventArgs e)
        {
            btn_SQLSyncAll.Visibility = btn_SQLBackup.Visibility = btn_SQLEnable.Visibility = btn_SQLTest.Visibility = Visibility.Visible;
        }
    }
}
