using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Web.UI;
using System.Windows;

namespace TheTimeApp.TimeData
{
    public class HtmlTimeReporter
    {
        public static string DaysToHtml(List<Day> days)
        {
            StringWriter stringWriter = new StringWriter();
            
            using (HtmlTextWriter htmlWriter = new HtmlTextWriter(stringWriter))
            {
                htmlWriter.RenderBeginTag("html");
                
                // Head
                htmlWriter.RenderBeginTag("head");
                htmlWriter.RenderBeginTag("title");
                htmlWriter.Write($"{TimeServer.SqlCurrentUser}'s Time Report");
                htmlWriter.RenderEndTag();
                htmlWriter.RenderEndTag();
                
                // Body
                htmlWriter.AddStyleAttribute("margin", "auto");
                htmlWriter.AddStyleAttribute("max-width", "1000");
                htmlWriter.AddStyleAttribute("max-width", "700");
                htmlWriter.RenderBeginTag("body");

                if (days.Count > 0)
                {
                    string minString = TimeServer.DateString(days.Min(d => d.Date));
                    string maxString = TimeServer.DateString(days.Max(d => d.Date));
                    
                    // Header
                    htmlWriter.AddStyleAttribute("color", "blue");
                    htmlWriter.RenderBeginTag("h1");
                    
                    // Header text
                    htmlWriter.AddAttribute("align", "center");
                    htmlWriter.RenderBeginTag("p");
                    htmlWriter.Write($"{TimeServer.SqlCurrentUser}'s time report <br> ({minString} - {maxString})");
                    htmlWriter.RenderEndTag();
                    
                    htmlWriter.RenderEndTag();
                
                    // List of time entries
                    htmlWriter.RenderBeginTag("ul");
                    foreach (Day day in days)
                    {
                        htmlWriter.RenderBeginTag("li");
                        
                        // Date
                        htmlWriter.AddAttribute("title", "Date");
                        htmlWriter.RenderBeginTag("font");
                        htmlWriter.Write(day.Date.DayOfWeek + " " +TimeServer.DateString(day.Date) + "<br>");
                        htmlWriter.RenderEndTag();
                        
                        // Hours
                        htmlWriter.AddAttribute("title", "Hours as decimal");
                        htmlWriter.RenderBeginTag("font");
                        htmlWriter.Write(day.HoursAsDecToQuarter + " hr.<br>");
                        htmlWriter.RenderEndTag();
                        
                        // Details
                        htmlWriter.AddAttribute("title", "Work details");
                        htmlWriter.RenderBeginTag("font");
                        htmlWriter.Write(day.Details + "<br>");
                        htmlWriter.RenderEndTag();
                        
                        // Divider
                        htmlWriter.RenderBeginTag("p");
                        htmlWriter.Write("\n");
                        htmlWriter.RenderEndTag();
                    }
                    htmlWriter.RenderEndTag();    
                
                    // Total hours
                    htmlWriter.AddAttribute("title", "Total hours as decimal");
                    htmlWriter.AddAttribute("align", "center");
                    htmlWriter.AddStyleAttribute("color", "red");
                    htmlWriter.RenderBeginTag("p");
                    htmlWriter.AddAttribute("font size", "6");
                    htmlWriter.RenderBeginTag("font");
                    htmlWriter.Write($"Total: {days.Sum(d => d.HoursAsDecToQuarter)}");
                    htmlWriter.RenderEndTag();
                    htmlWriter.RenderEndTag();
                }
                
                // Link
                htmlWriter.AddAttribute("align", "center");
                htmlWriter.RenderBeginTag("p");
                htmlWriter.Write("Brought to you by <a href = http://wrmcodeblocks.com/TheTimeApp/Downloads/ title = 'Link to download site'> TheTimeApp </a>.");
                htmlWriter.RenderEndTag();
                
                
                htmlWriter.RenderEndTag();
                htmlWriter.RenderEndTag();
            }

            return stringWriter.ToString();
        }

        public static string SaveReport(string html, DateTime dateA, DateTime dateB)
        {
            string reportLocation = TimeServer.AppDataDirectory + "\\Reports";
            string reportName = $"{TimeServer.SqlCurrentUser}-({TimeServer.DateSqLite(dateA)})-({TimeServer.DateSqLite(dateB)}).html";
            if (!Directory.Exists(reportLocation))
            {
                Directory.CreateDirectory(reportLocation);
            }

            if (File.Exists(Path.Combine(reportLocation, reportName)))
                File.Delete(Path.Combine(reportLocation, reportName));

            File.WriteAllText(Path.Combine(reportLocation, reportName), html);
            return Path.Combine(reportLocation, reportName);
        }

        public static void OnEmailWeek(DateTime date)
        {
            new Thread(() =>
            {
                try
                {
                    var startEndWeek = TimeServer.StartEndWeek(date);
                    DateTime start = startEndWeek[0];
                    DateTime end = startEndWeek[1];
                    var days = DataBaseManager.Instance.DaysInRange(start, end);

                    using (MailMessage msg = new MailMessage(AppSettings.Instance.FromAddress, AppSettings.Instance.ToAddress))
                    {
                        SmtpClient smtp = new SmtpClient();
                        NetworkCredential basicCredential = new NetworkCredential(AppSettings.Instance.FromUser, AppSettings.Instance.FromPass);
                        smtp.EnableSsl = AppSettings.Instance.SslEmail == "true";
                        smtp.Port = Convert.ToInt32(AppSettings.Instance.FromPort);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = basicCredential;
                        smtp.Host = AppSettings.Instance.EmailHost;
                        msg.Subject = "Time";
                        msg.Attachments.Add(new Attachment(SaveReport(DaysToHtml(days), start, end)));
                        msg.Body = "Auto generated time report.";
                        smtp.Send(msg);
                        MessageBox.Show("Mail sent!");    
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }).Start();
        }

        public static void OnPreviewWeek(DateTime date)
        {
            try
            {
                var startEndWeek = TimeServer.StartEndWeek(date);
                DateTime start = startEndWeek[0];
                DateTime end = startEndWeek[1];
                var days = DataBaseManager.Instance.DaysInRange(start, end);
                Process.Start(SaveReport(DaysToHtml(days), start, end));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }        
        }
    }
}