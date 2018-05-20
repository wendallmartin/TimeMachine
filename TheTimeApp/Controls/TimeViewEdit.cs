using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTimeApp.TimeData;
using Day = TheTimeApp.TimeData.Day;

namespace TheTimeApp
{
    public partial class TimeViewEdit : Form
    {
        
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        
        private Time _time = new Time();

        private TimeData.TimeData _data;
        
        public Time GetTime
        {
            get{ return _time; }
        }

        public DateTime GetDate
        {
            get{ return _time.TimeIn.Date; }
        }

        public TimeViewEdit(Time time, TimeData.TimeData data,  bool _12hour)
        {
            _data = data;
            _time = time;
            
            InitializeComponent();
            MouseDown += FormMouseDown;
            KeyDown += OnKeyDown;
            try
            {
                if (_12hour)
                {
                    timeInPicker.CustomFormat = "hh:mm tt";
                    timeOutPicker.CustomFormat = "hh:mm tt";
                }
                else
                {
                    timeInPicker.CustomFormat = "HH:mm";
                    timeOutPicker.CustomFormat = "HH:mm";
                }
                
                timeInPicker.Value = time.TimeIn;
                timeOutPicker.Value = time.TimeOut;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                DialogResult = DialogResult.Abort;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
            }
        }

        private void XButtonClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void bt_Save_Click(object sender, EventArgs e)
        {
            _time.TimeIn = timeInPicker.Value;
            _time.TimeOut = timeOutPicker.Value;
            DialogResult = DialogResult.OK;
        }
        
        private void FormMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
