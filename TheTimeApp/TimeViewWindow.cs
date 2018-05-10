using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using TheTimeApp.Controls;
using TheTimeApp.TimeData;
using Day = TheTimeApp.TimeData.Day;

namespace TheTimeApp
{
    public partial class TimeViewWindow : Form
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private const int WM_SETREDRAW = 11;


        public delegate void CloseDel(TimeData.TimeData data);

        public event CloseDel CloseEvent;

        private TimeData.TimeData _timeData;

        private int _lastScrollPos = 0;

        private bool _12hour = true;

        TimeViewEdit timeedit;

        private List<TimeView> timeViews;

        public TimeViewWindow(TimeData.TimeData timeData)
        {
            InitializeComponent();
            ViewToolStrip.MouseDown += ToolBarMouseDown;
            _timeData = timeData;

            _12hour = AppSettings.MilitaryTime == "true" ? false : true;
            InitTimes(true);
            TimeLayoutPanel.MouseEnter += UnHighlighAllTimeViews;
        }

        private void UnHighlighAllTimeViews(object sender, EventArgs e)
        {
            if (timeViews == null) return;

            foreach (TimeView timeView in timeViews)
            {
                timeView.UnSelect();
            }
        }
        
        private bool DatesAreInTheSameWeek(DateTime date1, DateTime date2)
        {
            var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
            var d1 = date1.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date1));
            var d2 = date2.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date2));

            return d1 == d2;
        }

        public void SuspendDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
        }

        public void ResumeDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
            parent.Refresh();
        }

        private void InitTimes(bool first = false)
        {
            SuspendDrawing(this);
            _lastScrollPos = TimeLayoutPanel.VerticalScroll.Value;

            timeViews = new List<TimeView>();

            TimeLayoutPanel.Controls.Clear();
            Day prev = new Day(new DateTime(2001,1,1));
            foreach (Day day in _timeData.Days)
            {
                if (!DatesAreInTheSameWeek(day.Date, prev.Date))
                {
                    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
                    var d2 = day.Date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(day.Date) + 1);
                    WeekViewBar weekViewBar = new WeekViewBar(new Size(Width - 31, 26), d2, _timeData.HoursInWeek(d2));
                    weekViewBar.DeleteWeekEvent += OnDeleteWeek;
                    weekViewBar.EmailWeekEvent += OnEmailWeek;
                    weekViewBar.PrintWeekEvent += OnPrintWeek;
                    weekViewBar.PreviewWeekEvent += OnPreviewWeek;
                    TimeLayoutPanel.Controls.Add(weekViewBar);
                }
                DayViewBar datevViewBar = new DayViewBar(new Size(Width - 31, 26), day);
                datevViewBar.MouseClickEvent += OnDateViewClick;
                datevViewBar.DeleteDayEvent += OnDeleteDayClick;
                TimeLayoutPanel.Controls.Add(datevViewBar);
                datevViewBar.Location = new Point(30, datevViewBar.Location.Y);

                foreach (Time time in day.Times)
                {
                    TimeView timeView = new TimeView(time, _12hour);
                    timeView.DeleteEvent += DeleteTime;
                    timeView.SelectedEvent += TimeViewSelected;
                    TimeLayoutPanel.Controls.Add(timeView);
                    timeViews.Add(timeView);
                }
                prev = day;
            }

            if (first)
            {
                if (TimeLayoutPanel.Controls.Count > 0)
                {
                    TimeLayoutPanel.ScrollControlIntoView(TimeLayoutPanel.Controls[TimeLayoutPanel.Controls.Count - 1]); 
                }
            }
                
            else
            {
                TimeLayoutPanel.VerticalScroll.Value = _lastScrollPos;
                TimeLayoutPanel.VerticalScroll.Value = _lastScrollPos;
            }
            ResumeDrawing(this);
        }

        private void OnDeleteWeek(DateTime date)
        {
            _timeData.DeleteWeek(date);
            InitTimes();
        }

        private void OnDeleteDayClick(DateTime date)
        {
            _timeData.DeleteDay(date);
            InitTimes();
        }

        private void OnDateViewClick(DateTime date)
        {
            Day day = _timeData.Days.First(d => d.Date == date);
            if (day == null) return;

            wpfDayViewEdit dayView = new wpfDayViewEdit(day);
            dayView.ShowDialog();

            _timeData.Save();

            InitTimes();
        }

        private void DeleteTime(Time time)
        {
            _timeData.DeleteTime(time);
            InitTimes();
        }

        private void TimeViewSelected(TimeView view)
        {
            int dayIndex = -1;
            int timeIndex = -1;
            for (int i = 0; i < _timeData.Days.Count; i++)
            {
                if (_timeData.Days[i].Contains(view.GetTime()))
                {
                    dayIndex = i;
                    for (int j = 0; j < _timeData.Days[i].Times.Count; j++)
                    {
                        if (_timeData.Days[i].Times[j] == view.GetTime())
                        {
                            timeIndex = j;
                        }
                    }
                    break;
                }
            }
            if (dayIndex == -1 || timeIndex == -1)
            {
                MessageBox.Show("Invalid operation!");
                Application.Exit();
            }

            timeedit = new TimeViewEdit(view.GetTime(), _12hour);

            var result = timeedit.ShowDialog();
            if (result == DialogResult.OK)
            {
                _timeData.Days[dayIndex].Date = timeedit.GetDate;
                _timeData.Days[dayIndex].Times[timeIndex] = timeedit.GetTime;
            }
            timeedit.Close();
            InitTimes();
        }

        private void XButton_Click(object sender, EventArgs e)
        {
            CloseEvent?.Invoke(_timeData);
        }

        private void OnEmailWeek(DateTime date)
        {
            new Thread(() =>
            {
                try
                {
                    MailMessage msg = new MailMessage(AppSettings.FromAddress, AppSettings.ToAddress);
                    SmtpClient smtp = new SmtpClient();
                    NetworkCredential basicCredential = new NetworkCredential(AppSettings.FromUser, AppSettings.FromPass);
                    smtp.EnableSsl = AppSettings.SslEmail == "true";
                    smtp.Port = Convert.ToInt32(AppSettings.FromPort);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = basicCredential;
                    smtp.Host = AppSettings.EmailHost;
                    msg.Subject = "Time";
                    msg.Body = _timeData.ConverWeekToText(date);
                    smtp.Send(msg);
                    MessageBox.Show("Mail sent!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }).Start();
        }

        private void OnPrintWeek(DateTime date)
        {
            PrintDocument p = new PrintDocument();
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(_timeData.ConverWeekToText(date), new Font("Times New Roman", 36), new SolidBrush(Color.Black),
                    new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));

            };
            try
            {
                PrintDialog pdp = new PrintDialog();
                pdp.Document = p;

                if (pdp.ShowDialog() == DialogResult.OK)
                {
                    p.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void OnPreviewWeek(DateTime date)
        {
            PrintDocument p = new PrintDocument();
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(_timeData.ConverWeekToText(date), new Font("Times New Roman", 36), new SolidBrush(Color.Black),
                    new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));

            };
            try
            {
                PrintPreviewDialog pdp = new PrintPreviewDialog();
                pdp.Document = p;

                if (pdp.ShowDialog() == DialogResult.OK)
                {
                    p.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void hToolStripMenuItem12hour_Click(object sender, EventArgs e)
        {
            AppSettings.MilitaryTime = "false";
            _12hour = true;
            InitTimes();
        }

        private void hToolStripMenuItem24hour_Click(object sender, EventArgs e)
        {
            AppSettings.MilitaryTime = "true";
            _12hour = false;
            InitTimes();
        }
                
        private void ToolBarMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
