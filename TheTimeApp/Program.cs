using System;
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
