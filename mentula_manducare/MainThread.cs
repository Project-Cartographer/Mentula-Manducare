using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using mentula_manducare;
using mentula_manducare.Classes;
using mentula_manducare.Objects;

namespace MentulaManducare
{
    class MainThread
    {
        private static int InputTop = 0;
        private static int InputLeft = 0;
        private static bool yes = true;
        public static Update Updater = new Update();
        public static DateTime UpdateInterval = DateTime.Now;
        private static Task ServerThread_;
        private static Task WebSocketThread_;
        private static Task InputThread_;
        #region Disable Quick Edit Const & Extern
        const uint ENABLE_QUICK_EDIT = 0x0040;
        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        #endregion


        public static string[] MutableErrors = new[]
        {
            //SignalR Error that just gets thrown no way to fix it
            "Microsoft.AspNet.SignalR.Hubs.ConnectionIdProxy",
            //Literally no idea where this comes from, stack trace goes to extern code and enabling the setting doesn't fix it
            //probably another SignalR thing
            "HKLM\\Software\\Microsoft\\Fusion!EnableLog",
            //This gets thrown even if it successfully loads the lib.. SignalR
            "Could not load file or assembly 'mscorlib.XmlSerializers",
            //This is thrown because the application is running a webserver without the proper 
            //preformance counters installed
            "The requested Performance Counter is not a custom counter",
            //It actually does load...
            ".Load"
        };

        static void Main(string[] args)
        {
            #region Disable Quick Edit
            //Gotta love new Features that probably break absolutely everything made in the past
            //https://stackoverflow.com/questions/13656846/how-to-programmatic-disable-c-sharp-console-applications-quick-edit-mode/36720802#36720802
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            uint consoleMode;
            GetConsoleMode(consoleHandle, out consoleMode);
            consoleMode &= ~ENABLE_QUICK_EDIT;
            SetConsoleMode(consoleHandle, consoleMode);
            #endregion
            #region Header
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "HHHHHHHHH     HHHHHHHHH  222222222222222     PPPPPPPPPPPPPPPPP            CCCCCCCCCCCCC"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "H:::::::H     H:::::::H 2:::::::::::::::22   P::::::::::::::::P        CCC::::::::::::C"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "H:::::::H     H:::::::H 2::::::222222:::::2  P::::::PPPPPP:::::P     CC:::::::::::::::C"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "HH::::::H     H::::::HH 2222222     2:::::2  PP:::::P     P:::::P   C:::::CCCCCCCC::::C"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "  H:::::H     H:::::H               2:::::2    P::::P     P:::::P  C:::::C       CCCCCC"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "  H:::::H     H:::::H               2:::::2    P::::P     P:::::P C:::::C              "));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "  H::::::HHHHH::::::H            2222::::2     P::::PPPPPP:::::P  C:::::C              "));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "  H:::::::::::::::::H       22222::::::22      P:::::::::::::PP   C:::::C              "));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "  H:::::::::::::::::H     22::::::::222        P::::PPPPPPPPP     C:::::C              "));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "  H::::::HHHHH::::::H    2:::::22222           P::::P             C:::::C              "));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "  H:::::H     H:::::H   2:::::2                P::::P             C:::::C              "));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "  H:::::H     H:::::H   2:::::2                P::::P              C:::::C       CCCCCC"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "HH::::::H     H::::::HH 2:::::2       222222 PP::::::PP             C:::::CCCCCCCC::::C"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "H:::::::H     H:::::::H 2::::::2222222:::::2 P::::::::P              CC:::::::::::::::C"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "H:::::::H     H:::::::H 2::::::::::::::::::2 P::::::::P                CCC::::::::::::C"));
            WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (89 / 2)) + "}", "HHHHHHHHH     HHHHHHHHH 22222222222222222222 PPPPPPPPPP                   CCCCCCCCCCCCC"));
            #endregion
            #region Error Catching
            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
            {

                if (!MutableErrors.Any(eventArgs.Exception.ToString().Contains))
                {
                    //if(eventArgs.Exception.ToString().Contains("SignalR"))
                    //  WebSocketThread.StartWebApp();
#if DEBUG
                    WriteLine(
                        $"An exception has occured within the application please check the error log in {Logger.LogBase}",
                        true);
#endif
                    Logger.AppendToLog("ErrorLog1", eventArgs.Exception.ToString());
                }

            };

            #endregion

            yes = false;
#if !DEBUG
            Updater.CheckUpdates();
#endif
            Console.Title = $"H2Pineapple {Updater.CurrentVersion}";

            //Starts the Websocket server
            WebSocketThread_ = Task.Factory.StartNew(WebSocketThread.Run);
            //Starts the server watching thread
            ServerThread_ = Task.Factory.StartNew(ServerThread.Run);
            //Moved Input to seperate Thread for performance reasons.
            InputThread_ = Task.Factory.StartNew(ConsoleInputThread.Run);

            while (true)
            {
#if !DEBUG
                if (DateTime.Now - UpdateInterval > TimeSpan.FromMinutes(10))
                {
                    Updater.CheckUpdates();
                    UpdateInterval = DateTime.Now;
                }
#endif
                if (WebSocketThread_.IsFaulted || WebSocketThread_.IsCompleted || WebSocketThread_.IsCanceled)
                    WebSocketThread_ = Task.Factory.StartNew(WebSocketThread.Run);
                if (ServerThread_.IsFaulted || ServerThread_.IsCompleted || ServerThread_.IsCanceled)
                    ServerThread_ = Task.Factory.StartNew(ServerThread.Run);
                Thread.Sleep(1000);
            }
        }

        public static void WriteLine(object Input, bool isInput = false)
        {

            //Console.SetCursorPosition(InputLeft, InputTop);
            //
            try
            {
                if (isInput == false) Console.ResetColor();
                if (yes) Console.ForegroundColor = ConsoleColor.Yellow;
                Console.SetCursorPosition(0, InputTop);
                Console.WriteLine(Input);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, InputTop + 1);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Enter Command:" + new string(' ', Console.WindowWidth - 16));

                InputTop++;
            }
            catch (Exception e)
            {
                InputTop = 0;
                Console.Clear();
            }
            //Console.SetCursorPosition(InputLeft, InputTop);
        }

        private static bool basePathCheck = false;
        public static string BasePath
        {
            get
            {
                if (!basePathCheck)
                {
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\"))
                        Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\");
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Logs"))
                        Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Logs");
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Settings"))
                        Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Settings");
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Update"))
                        Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Update");
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Messages"))
                        Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Messages");
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Stats"))
                        Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\Stats");
                    basePathCheck = true;
                }
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mentula\\";
            }
        }
    }
}
