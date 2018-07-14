using System;
using System.Collections.Generic;
using System.IO;

namespace TheTimeApp.TimeData
{
    public class Logger
    {
        public static void OverWriteToFile(List<string> messages, string fileName)
        {
            if (!Directory.Exists("log")) Directory.CreateDirectory("log");
            using (StreamWriter logger = new StreamWriter($"log\\{DateTime.Now.Date.Month}-{DateTime.Now.Date.Day}-{DateTime.Now.Date.Year}_{fileName}.log"))
            {
                foreach (string message in messages)
                {
                    logger.WriteLine(message);
                }
            }
        }

        public static void LogException(Exception e, string fileName)
        {
            if (!Directory.Exists("log")) Directory.CreateDirectory("log");
            using (StreamWriter logger = new StreamWriter($"log\\{DateTime.Now.Date.Month}-{DateTime.Now.Date.Day}-{DateTime.Now.Date.Year}_{fileName}.log", true))
            {
                logger.WriteLine($"{DateTime.Now}: {e.Message}");
            }
        }   
    }
}