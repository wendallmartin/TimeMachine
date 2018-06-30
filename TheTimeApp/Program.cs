using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
            var processes = Process.GetProcessesByName("TheTimeApp");
            List<Process> orderedEnumerable = new List<Process>(processes.OrderBy(p => p.StartTime));
            for(int i = 0; i < orderedEnumerable.Count() - 1; i++)
            {
                orderedEnumerable[i].Kill();
            }
            
            UpDater.RemoveOldMoveNewFiles();
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
    }
}
