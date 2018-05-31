using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TheTimeApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (AppSettings.MainPermission == "write")
            {
                WPFTimeAppForm mytime = new WPFTimeAppForm();
                mytime.ShowDialog();
            }
            else
            {
                WPFTimeViewForm myview = new WPFTimeViewForm();
                myview.ShowDialog();
            }
        }
    }
}
