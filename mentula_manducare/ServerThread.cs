using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mentula_manducare.App_Code;
using mentula_manducare.Classes;
using mentula_manducare.Objects;
using MentulaManducare;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;

namespace mentula_manducare
{

    public static class ServerThread
    {


        public static string ExecutionPath { get; set; }

        public static string PlaylistFolder =>
            $"{ExecutionPath}\\Playlists\\";

        public static ServerCollection Servers = new ServerCollection();
        public static int TickCount = 0;
        public static TimeSpan TPS = TimeSpan.Zero;
        public static int TPT = 0;
        public static int TPC = 0;
        public static int SleepTime = 10;
        public static void Run()
        {

            var watch = Stopwatch.StartNew();
            DetectServers(true);
            while (true)
            {
                

                foreach (ServerContainer serverContainer in Servers)
                    serverContainer.Tick();

                Thread.Sleep(SleepTime);
                
                TPS = TPS.Add(watch.Elapsed);
                watch.Restart();
                TickCount++;
                if (TPS.Seconds >= 1)
                {
                    var newTime = Math.Floor(SleepTime + (SleepTime - (30f * (SleepTime / TickCount))));
                    if (newTime < 0)
                        newTime = 1;
                    //MainThread.WriteLine($"Server Thread Tickrate: {TickCount}");
                    SleepTime = (int)newTime;
                    TPT += TickCount;
                    TickCount = 0;
                    TPS = TimeSpan.Zero;
                    TPC++;
                    if(TPC == 10)
                    {
                        TPC = 0;
                        //MainThread.WriteLine($"Average Server Thread Tickrate: {TPT / 10}/10s");
                        TPT = 0;
                    }
                    DetectServers();
                }


            }
        }

        public static void DetectServers(bool Inital = false)
        {
            var serverProcesses = Process.GetProcessesByName("h2Server");
            if (Inital)
            {
                MainThread.WriteLine($"Servers Detected:\t{serverProcesses.Length}");
                if (serverProcesses.Length > 0)
                {
                    ExecutionPath = serverProcesses[0].MainModule?.FileName.Replace("\\h2server.exe", "")
                        .Replace("\\H2Server.exe", "");
                    MainThread.WriteLine($"Server Launch Path:\t{ExecutionPath}");
                    MainThread.WriteLine($"Server Playlist Folder:\t{PlaylistFolder}");
                }
            }
            else
            {
                for (var i = 0; i < Servers.Count; i++)
                {
                    if (Servers[i].ServerProcess.HasExited)
                    {
                        if (!Servers[i].AutoRestart)
                        {
                            MainThread.WriteLine($"{Servers[i].FormattedName} has closed detaching..");
                            Servers[i].KillConsoleProxy();
                            Servers.RemoveAt(i);
                        }
                        else
                        {
                            MainThread.WriteLine($"{Servers[i].FormattedName} has closed restarting service..");
                            var Service = new ServiceController(Servers[i].ServiceName);
                            if (Service.Status != ServiceControllerStatus.Stopped)
                            {
                                Service.Stop();
                                Servers[i].ServerProcess.Kill();
                            }
                            Service.Start();
                            Servers[i].KillConsoleProxy();
                            Servers.RemoveAt(i);
                        }
                    }

                }
            }
            for (var i = 0; i < serverProcesses.Length; i++)
            {
                if (!Servers.ServerCollected(serverProcesses[i]))
                {
                    MainThread.WriteLine($"Attaching To Server...");
                    var newServer = new ServerContainer(serverProcesses[i]);
                    newServer.Index = i;
                    if (newServer.isLive)
                    {
                        MainThread.WriteLine($"Attached to: {newServer.FormattedName}");
                        MainThread.WriteLine($"Service Name: {newServer.ServiceName}");
                        newServer.LaunchConsoleProxy();
                        Servers.Add(newServer);
                    }
                    else
                        MainThread.WriteLine(
                            $"Skipping LAN Server: {newServer.FormattedName}");
                }
            }
        }
    }
}
