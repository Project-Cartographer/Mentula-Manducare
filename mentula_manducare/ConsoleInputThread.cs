using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mentula_manducare.Objects;
using MentulaManducare;

namespace mentula_manducare
{
    public static class ConsoleInputThread
    {
        public static void Run()
        {
            while (true)
            {
                var input = Console.ReadLine();
                MainThread.WriteLine($"Command: {input}", true);
                ExecuteCommand(input);
            }

        }
        public static void ExecuteCommand(string input)
        {
            string Command = input.Split(' ').First();
            List<string> Params = input.Split(' ').ToList();
            Params.RemoveAt(0);
            switch (Command.ToLower())
            {
              
                case "adduser":
                    {
                        if (Params.Count != 2)
                            MainThread.WriteLine("Incorrect amount of parameters provided, please provide a username and password",
                                true);
                        else
                        {
                            WebSocketThread.Users.Add(Params[0], Params[1]);
                            MainThread.WriteLine($"User {Params[0]} added", true);
                        }

                        break;
                    }
                case "removeuser":
                    {
                        if (Params.Count != 1)
                            MainThread.WriteLine("Incorrect amount of parameters provided, please provide a username", true);
                        else
                        {
                            WebSocketThread.Users.Remove(Params[0]);
                            MainThread.WriteLine($"User {Params[0]} Removed", true);
                        }

                        break;
                    }
                case "listuser":
                    {
                        MainThread.WriteLine("Users:", true);
                        WebSocketThread.Users.AsList.ForEach((user => { MainThread.WriteLine(user.Username, true); }));
                        break;
                    }
                case "resetuser":
                    {
                        MainThread.WriteLine(
                            WebSocketThread.Users.ResetPassword(Params[0], Params[1])
                                ? $"Password reset for {Params[0]}"
                                : "Invalid username",
                            true);
                        break;
                    }
                case "listserver":
                    {
                        MainThread.WriteLine($"==Listing {ServerThread.Servers.Count} Servers==", true);
                        foreach (ServerContainer server in ServerThread.Servers)
                        {
                            MainThread.WriteLine($"Index: {server.FormattedName}", true);
                        }
                        break;
                    }
                case "stopserver":
                    {
                        if(Params.Count != 0)
                            if (int.Parse(Params[0]) <= ServerThread.Servers.Count)
                            {
                                MainThread.WriteLine(
                                    $"Stopping Server {Params[0]}, Service will have to be restarted manually", true);
                                ServerThread.Servers[int.Parse(Params[0])].KillServer(false);
                            }
                            else
                            {
                                MainThread.WriteLine($"Invalid Server Index given.");
                            }

                        break;
                    }
                case "restartserver":
                    {
                        if (int.Parse(Params[0]) <= ServerThread.Servers.Count)
                        {
                            MainThread.WriteLine($"Restarting Server {Params[0]}....", true);
                            ServerThread.Servers[int.Parse(Params[0])].KillServer();
                        }
                        else
                        {
                            MainThread.WriteLine($"Invalid Server Index given.");
                        }
                        break;
                    }
                case "savestats":
                    {
                        if (int.Parse(Params[0]) <= ServerThread.Servers.Count)
                        {
                            MainThread.WriteLine($"Printing Stats for server {Params[0]}...", true);
                            ServerThread.Servers[int.Parse(Params[0])].CarnageReport.SaveJSON();
                        }
                        else
                        {
                            MainThread.WriteLine($"Invalid Server Index given.");
                        }
                        break;
                    }
                default:
                    {
                        MainThread.WriteLine("Invalid Command", true);
                        break;
                    }
            }
        }

    }
}
