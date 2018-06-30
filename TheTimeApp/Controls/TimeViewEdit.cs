using System;
using System.Windows.Forms;
using TheTimeApp.TimeData;

namespace TheTimeApp
{
    public partial class TimeViewEdit : Form
    {
        
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WmNclbuttondown = 0xA1;
        public const int HtCaption = 0x2;

        public Time GetTime { get; }

        public TimeViewEdit(Time time, bool _12hour)
        {
            GetTime = time;
            
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
            GetTime.TimeIn = timeInPicker.Value;
            GetTime.TimeOut = timeOutPicker.Value;
            DialogResult = DialogResult.OK;
        }
        
        private void FormMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WmNclbuttondown, HtCaption, 0);
            }
        }
    }
}
