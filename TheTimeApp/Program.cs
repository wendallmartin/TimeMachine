using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using NLog;

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
            
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Path.Combine(Directory.GetCurrentDirectory(), "FTPUpdater.exe");
            info.Arguments = CurrentVersion + $" \"{Directory.GetCurrentDirectory()}\"" + " TheTimeApp false";
            Process.Start(info);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                if (AppSettings.MainPermission == "write")
                {
                    _log.Info($"TimeApp {CurrentVersion} run......");
                    _mytime = new WPFTimeAppForm();
                    _mytime.ShowDialog();
                    _log.Info("TimeApp run......FINISHED!!!");
                }
                else
                {
                    _log.Info($"TimeView: {CurrentVersion} run......");
                    _myview = new WPFTimeViewForm();
                    _myview.ShowDialog();
                    _log.Info("TimeView run......FINISHED!!!");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
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
