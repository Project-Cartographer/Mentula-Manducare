using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mentula_manducare.Enums;
using mentula_manducare.Objects;
using MentulaManducare;

namespace mentula_manducare.Classes
{
    public class ConsoleProxy
    {
        public Process Console_;
        public StreamWriter InputWriter;
        public StreamReader OutputReader;
        private string Instance;
        public string ResponseData = "";
        private int ExpectedLines = 8; //8 For Console Start..
        private int LineCount = 0;
        private string DataOut = "";
        private bool Lock = true;

        public ConsoleProxy(string Instance)
        {
            MainThread.WriteLine($"Creating H2Admin Proxy for: {Instance}");
            this.Instance = Instance;
            Console_ = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = $"{ServerThread.ExecutionPath}\\h2admin.exe",
                    Arguments = $"-live {Instance}",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            MainThread.WriteLine(
                $"Starting H2Admin Proxy for: {Instance} {Environment.NewLine} \tUsing Arguments {Console_.StartInfo.Arguments}");
            Console_.Start();
            InputWriter = Console_.StandardInput;
            //OutputReader = Console_.StandardOutput;
            //Console_.OutputDataReceived += ReadOutput;
            //Console_.BeginOutputReadLine();

        }

        public void KickPlayer(string PlayerName)
        {
            
            InputWriter.WriteLine($"kick \"{PlayerName}{Environment.NewLine}");
        }

        public void BanPlayer(string PlayerName)
        {
            InputWriter.WriteLine($"ban gamer \"{PlayerName}{Environment.NewLine}");
        }
        public void UnBanPlayer(string PlayerName)
        {
            InputWriter.WriteLine($"unban gamer \"{PlayerName}{Environment.NewLine}");
        }

        public void SendMessage(string Message)
        {
            InputWriter.WriteLine($"sendmsg \"{Message}{Environment.NewLine}");
        }
        public void Skip()
        {
            InputWriter.WriteLine($"skip{Environment.NewLine}");
        }

        public void SetPlaylist(string playlist)
        {
            InputWriter.WriteLine($"play \"{playlist}\"{Environment.NewLine}");
        }

        public void Privacy(Privacy privacy)
        {
            InputWriter.WriteLine($"privacy \"{privacy.ToString()}\"{Environment.NewLine}");
        }

        public void Players(int count)
        {
            InputWriter.WriteLine($"players {count.ToString()}{Environment.NewLine}");
        }

        public void AddVIP(string PlayerName)
        {
            InputWriter.WriteLine($"vip add \"{PlayerName}");
        }

        public void RemoveVIP(string PlayerName)
        {
            InputWriter.WriteLine($"vip remove \"{PlayerName}");
        }

        public void ClearVIP()
        {
            InputWriter.WriteLine($"vip clear");
        }



        public void Status()
        {
            InputWriter.WriteLine("status");
        }
        public void Kill(int pid = -1)
        {
            if (pid == -1)
                pid = Console_.Id;
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
                ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    Kill(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }
        }
        //public void ReadOutput(object sendingProcess, DataReceivedEventArgs output)
        //{

        //    ResponseData += output.Data;
        //    LineCount++;
        //    MainThread.WriteLine(output.Data + " " + LineCount.ToString() + "/" + ExpectedLines);
        //    if (LineCount == ExpectedLines || LineCount > ExpectedLines)
        //    {
        //        MainThread.WriteLine("END");
        //        Lock = false;
        //        DataOut = ResponseData;
        //        ResponseData = "";
        //        LineCount = 0;
        //        //Console_.CancelOutputRead();
        //        //Console_.StandardOutput.DiscardBufferedData();
        //    }
        //}
        //public string GetStatusValue(string Val = "")
        //{
        //    while (Lock)
        //    {
        //    }

        //    Lock = true;
        //    DataOut = "";
        //    MainThread.WriteLine("A");
        //    ExpectedLines = 14;
        //    //Console_.BeginOutputReadLine();
        //    MainThread.WriteLine("B");
        //    Console_.StandardInput.WriteLine($"status {Environment.NewLine}");
        //    MainThread.WriteLine("C");
        //    while (DataOut == "")
        //    { }
        //    MainThread.WriteLine("B");
        //    if (Val == "")
        //    {
        //        MainThread.WriteLine("C");
        //        return DataOut;
        //    }
        //    else
        //    {
        //        MainThread.WriteLine("D");
        //        foreach (string s in DataOut.Split('\n'))
        //        {
        //            if (s.Split(new string[] {": "}, StringSplitOptions.None)[0].ToLower() == Val.ToLower())
        //            {
        //                return s.Split(new string[] {": "}, StringSplitOptions.None)[1].Replace(" online - ", "");
        //            }

        //            if (Val == "Playing")
        //                if (s.Contains("Playing "))
        //                {
        //                    return s.Replace("Playing ", "Playing: ");
        //                }
        //        }
        //    }
        //    MainThread.WriteLine("E");
        //    return "SAY WA!";
        //}
    }
}
