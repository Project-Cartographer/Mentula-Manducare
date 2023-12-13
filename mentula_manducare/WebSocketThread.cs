using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mentula_manducare.Objects;
using MentulaManducare;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Owin;

namespace mentula_manducare
{
    public static class WebSocketThread
    {
        public static UserCollection Users;
        public static IDisposable SignalRDisposable = null;
        public static void Run()
        {
            StartWebApp();
            Users = new UserCollection();
            while (true)
            {
                //Any Background tasks go here
                Thread.Sleep(30000);

            }
        }

        public static void StartWebApp()
        {
            SignalRDisposable?.Dispose();
            string url = "http://+:9922";
            SignalRDisposable = WebApp.Start(url, Startup.Configuration);
            MainThread.WriteLine($"SignalR Server running on {url}", true);
        }
    }

    internal static class Startup
    {
        private static bool GlobalConfig = false;
        public static void Configuration(IAppBuilder app)
        {
            var hubConfiguration = new HubConfiguration();
            hubConfiguration.EnableDetailedErrors = false;
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR("/signalr", hubConfiguration);
            if (!GlobalConfig)
            {
                GlobalConfig = true;
                GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(60);
                GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);
                GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds((int)Math.Floor(60F / 3F));
            }
        }
    }
   
}
