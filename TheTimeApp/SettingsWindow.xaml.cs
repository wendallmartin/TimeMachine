using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using Day = TheTimeApp.TimeData.Day;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace TheTimeApp
{
    /// <summary>
    /// Interaction logic for w.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private readonly object _update = new object();
        private bool _changeDataPath;        
        public SettingsWindow()
        {
            InitializeComponent();

            TextBoxFromAddress.Text = AppSettings.Instance.FromAddress;
            TextBoxUserName.Text = AppSettings.Instance.FromUser;
            TextBoxFromPass.Text = "*********";
            TextBoxEmailServer.Text = AppSettings.Instance.EmailHost;
            TextBoxFromPort.Text = AppSettings.Instance.FromPort;
            TextBoxToAddress.Text = AppSettings.Instance.ToAddress;
            CheckBoxSsl.IsChecked = AppSettings.Instance.SslEmail == "true";

            TextBoxAzureDataSource.Text = AppSettings.Instance.AzureDateSource;
            TextBoxAzureUserId.Text = AppSettings.Instance.AzureUser;
            TextBoxAzurePassword.Text = "*********";
            TextBoxAzureCatelog.Text = AppSettings.Instance.AzureCateloge;
            TextBoxAzurePort.Text = AppSettings.Instance.AzurePort;
            
            TextBoxMySqlDataSource.Text = AppSettings.Instance.MySqlServer;
            TextBoxMySqlUserId.Text = AppSettings.Instance.MySqlUserId;
            TextBoxMySqlPassword.Text = "*********";
            TextBoxMySqlPort.Text = AppSettings.Instance.MySqlPort.ToString();
            TextBoxMySqlDatabase.Text = AppSettings.Instance.MySqlDatabase;
            
            btn_SQLEnable.Content = AppSettings.Instance.SqlEnabled == "true" ? "Enabled" : "Disabled";
            btn_Permission.Content = AppSettings.Instance.MainPermission == "write" ? "Write" : "Read";

            SqlTypeExpaner.Header = AppSettings.Instance.SqlType;

            switch (AppSettings.Instance.SqlType)
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
                        AppSettings.Instance.SqlType = "Azure";
                        MySqlSettings.Visibility = Visibility.Hidden;
                        AzureSettings.Visibility = Visibility.Visible;
                        return;
            }

            AssociateEvents();
            LoadUsers();

            if(DataBaseManager.Instance.ProgressChangedEvent == null) DataBaseManager.Instance.ProgressChangedEvent += OnSqlProgressChanged;
            if(DataBaseManager.Instance.ProgressFinishEvent == null) DataBaseManager.Instance.ProgressFinishEvent += OnProgressFinish;
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
            AppSettings.Instance.FromAddress = TextBoxFromAddress.Text;
        }

        private void TextBox_UserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.FromUser = TextBoxUserName.Text;
        }

        private void TextBox_FromPass_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.FromPass = TextBoxFromPass.Text;
        }

        private void TextBox_EmailServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.EmailHost = TextBoxEmailServer.Text;
        }

        private void TextBox_FromPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.FromPort = TextBoxFromPort.Text;
        }

        private void TextBox_ToAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.ToAddress = TextBoxToAddress.Text;
        }

        private void Ssl_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.Instance.SslEmail = "true";
        }

        private void Ssl_CheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            AppSettings.Instance.SslEmail = "false";
        }

        private void Btn_SQLEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.Instance.SqlEnabled == "true")
            {
                AppSettings.Instance.SqlEnabled = "false";
                btn_SQLEnable.Content = "Disabled";
            }
            else
            {
                AppSettings.Instance.SqlEnabled = "true";
                btn_SQLEnable.Content = "Enabled";
            }
        }
        
        /// <summary>
        /// Updates progress bar with given percentage.
        /// </summary>
        /// <param name="percentage"></param>
        private void OnSqlProgressChanged(float percentage)
        {
            Dispatcher.Invoke(() => { ProgressBar_SQLRePushAll.Value = percentage;});
        }
        
        private void OnProgressFinish()
        {
            Dispatcher.Invoke(() =>
            {
                btn_SQLBackup.IsEnabled = btn_SQLSyncAll.IsEnabled = true;
                ProgressBar_SQLRePushAll.Value = 0;
                ProgressBar_SQLRePushAll.Visibility = Visibility.Hidden;
            });
        }


        private void btn_EmployEmployer_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.Instance.MainPermission == "write")
            {
                AppSettings.Instance.MainPermission = "read";
                btn_Permission.Content = "Read";
            }
            else
            {
                AppSettings.Instance.MainPermission = "write";
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
                        info.FileName = Path.Combine(Directory.GetCurrentDirectory(), "Updater.exe");
                        info.Arguments = Program.CurrentVersion + $" \"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheTimeApp")}\" http://www.wrmcodeblocks.com/TheTimeApp/Downloads";
                        Process.Start(info);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show($"Cannot update! {exception.Message}.");
                    }
                }
            }).Start();
        }
        
        private void DataLocation_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Time file (*.sqlite)|*.sqlite|Dtf (*.dtf)|*.dtf|Tdf (*.tdf)|*.tdf";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (AppSettings.Instance.DataPath != openFileDialog.FileName)
                {
                    _changeDataPath = true;
                    AppSettings.Instance.DataPath = openFileDialog.FileName;    
                }
            }
        }
        
        #region sql settings
        
        private void Btn_SQLPushAll_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.Instance.SqlEnabled != "true")
            {
                MessageBox.Show("SQL not enabled!");
                return;
            }
            
            ProgressBar_SQLRePushAll.Visibility = Visibility.Visible;
            btn_SQLSyncAll.IsEnabled = false;
            new Thread(() =>
            {
                DataBaseManager.Instance.PushPrimaryToSecondary();
                Dispatcher.Invoke(() =>
                {
                    btn_SQLSyncAll.IsEnabled = true;
                    ProgressBar_SQLRePushAll.Visibility = Visibility.Hidden;
                });
                
            }).Start();
        }
        
        private void Btn_SQLDownload_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.Instance.SqlEnabled != "true")
            {
                MessageBox.Show("SQL not enabled!");
                return;
            }

            if (MessageBox.Show("Local data will be overwritten! \n Are you sure?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                btn_SQLBackup.IsEnabled = false;
                ProgressBar_SQLRePushAll.Visibility = Visibility.Visible;
                new Thread(() =>
                {
                    DataBaseManager.Instance.PullSecondaryToPrimary();
                    Dispatcher.Invoke(() =>
                    {
                        btn_SQLBackup.IsEnabled = true;
                        MessageBox.Show("Download successful!");
                        ProgressBar_SQLRePushAll.Visibility = Visibility.Hidden;
                    });
                }).Start();    
            }
        }

        private void BtnTestClick(object sender, RoutedEventArgs e)
        {

            try
            {
                switch (AppSettings.Instance.SqlType)
                {
                    case "Azure":
                        SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder()
                        {
                            DataSource = AppSettings.Instance.AzureDateSource,
                            UserID = AppSettings.Instance.AzureUser,
                            Password = AppSettings.Instance.AzurePassword,
                            InitialCatalog = AppSettings.Instance.AzureCateloge,
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
                            Server = AppSettings.Instance.MySqlServer,
                            UserID = AppSettings.Instance.MySqlUserId,
                            Password = AppSettings.Instance.MySqlPassword,
                            Port = (uint) AppSettings.Instance.MySqlPort,
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
            SqlTypeExpaner.Header = AppSettings.Instance.SqlType = "Azure";
            MySqlSettings.Visibility = Visibility.Hidden;
            AzureSettings.Visibility = Visibility.Visible;
            SqlTypeExpaner.IsExpanded = false;
        }

        private void BtnTypeMySql_Click(object sender, RoutedEventArgs e)
        {
            SqlTypeExpaner.Header = AppSettings.Instance.SqlType = "MySql";
            AzureSettings.Visibility = Visibility.Hidden;
            MySqlSettings.Visibility = Visibility.Visible;
            SqlTypeExpaner.IsExpanded = false;
        }

        #region azure value change

        private void TextBoxAzureDataSource_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.AzureDateSource = TextBoxAzureDataSource.Text;
        }

        private void TextBoxAzureUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.AzureUser = TextBoxAzureUserId.Text;
        }

        private void TextBoxAzurePassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.AzurePassword = TextBoxAzurePassword.Text;
        }

        private void TextBoxAzureCatelog_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.AzureCateloge = TextBoxAzureCatelog.Text;
        }
        
        private void TextBoxAzurePort_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.AzurePort = (sender as TextBox)?.Text;
        }
        
        private void TextBoxMySqlPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.MySqlPort = int.Parse(TextBoxMySqlPort.Text);
        }

        private void TextBoxMySqlPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.MySqlPassword = TextBoxMySqlPassword.Text;
        }

        private void TextBoxMySqlUserId_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.MySqlUserId = TextBoxMySqlUserId.Text;
        }

        private void TextBoxMySqlDataSource_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.MySqlServer = TextBoxMySqlDataSource.Text;
        }
        
        private void TextBoxMySqlDatabase_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Instance.MySqlDatabase = TextBoxMySqlDatabase.Text;
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

        private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            DataBaseManager.Instance.SaveBuffer();
            if (_changeDataPath)
            {
                AppSettings.Instance.Save();
                Thread thread = new Thread (() => { Process.Start("TheTimeApp.exe");});
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
        }
    }
}
