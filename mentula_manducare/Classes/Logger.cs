using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentulaManducare;

namespace mentula_manducare.Classes
{
    public static class Logger
    {
        public static string LogBase = $"{MainThread.BasePath}Logs\\";
        private static List<LoggerBase> Logs = new List<LoggerBase>();
        public static void AppendToLog(string logName, string logText)
        {
           GetLog(logName).AppendLog(logText);
        }

        public static LoggerBase CreateLog(string logName)
        {
            Logs.Add(new LoggerBase(logName));
            return Logs[Logs.Count - 1];
        }
        public static bool LogExists(string logName)
        {
            return Logs.SingleOrDefault(x => x.logName == logName) != null;
        }

        public static LoggerBase GetLog(string logName)
        {
            if (LogExists(logName))
                return Logs.SingleOrDefault(x => x.logName == logName);
            else
                return CreateLog(logName);
        }
    }
    public class LoggerBase
    {
        private FileStream fileSteam;
        private StreamWriter streamWriter;
        private StreamReader streamReader;
        public string logName { get; set; }
        public LoggerBase(string logName)
        {
            this.logName = logName;
            fileSteam = new FileStream($"{Logger.LogBase}{logName}.log", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            streamWriter = new StreamWriter(fileSteam);
            streamReader = new StreamReader(fileSteam);
        }

        public void AppendLog(string logText)
        {
            fileSteam.Seek(fileSteam.Length, SeekOrigin.Begin);
            streamWriter.WriteLine($"[{DateTime.Now.ToString()}]: {logText}");
            streamWriter.Flush();
            fileSteam.Flush(true);
        }

        public string[] DumpLogs()
        {
            streamReader.BaseStream.Position = 0;
            return streamReader.ReadToEnd().Split('\n');
        }
        
    }
}
