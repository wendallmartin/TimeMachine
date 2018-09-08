using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NLog;
using TheTimeApp.TimeData;
using Day = System.Windows.Forms.Day;

namespace TheTimeApp
{
    static class Program
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private static WPFTimeAppForm _mytime;
        private static WPFTimeViewForm _myview;
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            foreach (Process process in Process.GetProcessesByName("TheTimeApp"))
            {
                if(process.StartTime != Process.GetCurrentProcess().StartTime)
                    process.Kill();
            }

            if (File.Exists("Updater.exe"))
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "Updater.exe";
                info.Arguments = CurrentVersion + $" \"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheTimeApp")}\" http://www.wrmcodeblocks.com/TheTimeApp/Downloads false";
                Process.Start(info);    
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                AppSettings.Validate();
                
                // set user name if not specified
                while (string.IsNullOrEmpty(AppSettings.CurrentUser))
                {
                    EnterUser userWin = new EnterUser();
                    userWin.ShowDialog();
                    AppSettings.CurrentUser = userWin.txt_User.Text;
                }
                
                TimeServer.SqlCurrentUser = AppSettings.CurrentUser;
                
                DataBaseManager.Initulize();

                if (!DataBaseManager.Instance.UserNames().Contains(TimeServer.SqlCurrentUser))
                {
                    DataBaseManager.Instance.AddUser(new User(TimeServer.SqlCurrentUser, "", new List<TimeData.Day>()));
                }
                
                if (AppSettings.MainPermission == "read")
                {
                    _log.Info($"TimeView: {CurrentVersion} run......");
                    _myview = new WPFTimeViewForm();
                    _myview.ShowDialog();
                    _log.Info("TimeView run......FINISHED!!!");
                }
                else
                {
                    _log.Info($"TimeApp {CurrentVersion} run......");
                    _mytime = new WPFTimeAppForm();
                    _mytime.ShowDialog();
                    _log.Info("TimeApp run......FINISHED!!!");   
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }
        
        //using System.Deployment.Application;
        //using System.Reflection;
        public static string CurrentVersion
        {
            get
            {
                return ApplicationDeployment.IsNetworkDeployed
                    ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                    : Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        } 
    }
}
