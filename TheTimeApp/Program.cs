using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace TheTimeApp
{
    static class Program
    {
        private static WPFTimeAppForm _mytime;
        private static WPFTimeViewForm _myview;
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
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
                    _mytime = new WPFTimeAppForm();
                    _mytime.ShowDialog();
                }
                else
                {
                    _myview = new WPFTimeViewForm();
                    _myview.ShowDialog();
                    TimeData.TimeData.TimeDataBase.Save();
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
