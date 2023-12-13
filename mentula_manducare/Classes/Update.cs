using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Windows.Forms;
using MentulaManducare;

namespace mentula_manducare.Classes
{
    public class Update
    {
        public static string RemoteBaseURI = "https://halo2pc.com/mentula/monitor/";
#if !DEBUG
        public string RemoteVersionURI = $"{RemoteBaseURI}latest.txt";
#else
        public string RemoteVersionURI = $"{RemoteBaseURI}latestdev.txt";
#endif
        public static string BasePath = $"{MainThread.BasePath}\\Update\\";
        public string LocalVersionPath = $"{BasePath}latest.txt";
        public string RemoteVersionPath = $"{BasePath}latest_tmp.txt";
        public string CurrentVersion = "";
        public bool UpdateRequired = false;
        public void GetLatestVersionInfo()
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(RemoteVersionURI, RemoteVersionPath);
                if (!File.Exists(LocalVersionPath))
                {
                    UpdateRequired = true;
                    File.Move(RemoteVersionPath, LocalVersionPath);
                }
                else
                {
                    var localLines = File.ReadAllLines(LocalVersionPath);
                    var remoteLines = File.ReadAllLines(RemoteVersionPath);
                    CurrentVersion = localLines[0];
                    if (localLines[0] != remoteLines[0])
                    {
                        UpdateRequired = true;
                        File.Delete(LocalVersionPath);
                        File.Move(RemoteVersionPath, LocalVersionPath);
                    }
                }
            }
        }

        public void DownloadUpdateFiles()
        {
            using (var client = new WebClient())
            {
                var localLines = File.ReadAllLines(LocalVersionPath);
                client.DownloadFile($"{RemoteBaseURI}{localLines[1]}", $"{BasePath}latest.zip");
                if (!Directory.Exists($"{BasePath}Temp"))
                    Directory.CreateDirectory($"{BasePath}Temp");
                else
                {
                    Directory.Delete($"{BasePath}Temp", true);
                    Directory.CreateDirectory($"{BasePath}Temp");
                }
                ZipFile.ExtractToDirectory($"{BasePath}latest.zip", $"{BasePath}Temp");
            }
        }

        public string GenerateUpdateBatch()
        {
            var LanchPath = Application.StartupPath;
            var exePath = (Application.ExecutablePath.ToLower().Contains("mentula")
                ? Application.StartupPath + "\\H2Pineapple.exe"
                : Application.ExecutablePath);
            var pause = "& ping 127.0.0.1 -n 1 -w 5000 > Nul & ";
            var result = $"/C ping 127.0.0.1 -n 1 -w 5000 > Nul & ";
            result += $"Del /F /Q /S {LanchPath}\\* {pause}"; //Delete current Files
            result += $"move /Y \"{BasePath}Temp\\*\" \"{LanchPath}\" {pause}";
            result += $"start \"\" \"{exePath}\"";

            //Fucking lazy shit
            //Comes out to "Pause -> Delete current version -> pause -> Move new version into cd -> pause -> start new version"
            return result;
        }
        public void CheckUpdates()
        {
            try
            {
                GetLatestVersionInfo();
                if (UpdateRequired)
                {
                    MainThread.WriteLine("Updating Tool..", true);
                    DownloadUpdateFiles();
                    MainThread.WriteLine("Update Files Downloaded", true);
                    var Batch = GenerateUpdateBatch();
                    MainThread.WriteLine("Beginning update process");
                    ProcessStartInfo Info = new ProcessStartInfo();
                    Info.FileName = "cmd.exe";
                    Info.WorkingDirectory = Application.StartupPath;
                    Info.Arguments = Batch;
                    Info.WindowStyle = ProcessWindowStyle.Hidden;
                    Info.CreateNoWindow = true;
                    Process.Start(Info);
                    Process.GetCurrentProcess().Kill();
                }
            }
            catch (Exception) { }
        }
    }
}
