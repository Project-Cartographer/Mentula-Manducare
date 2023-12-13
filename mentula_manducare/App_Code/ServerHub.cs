using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using mentula_manducare.Classes;
using mentula_manducare.Enums;
using mentula_manducare.Objects;
using MentulaManducare;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;

namespace mentula_manducare.App_Code
{
    [HubName("ServerHub")]
    public class ServerHub : Hub
    {
        public dynamic CurrentUser =>
            Clients.Client(Context.ConnectionId);

        public string CurrentToken =>
            Context.QueryString["connectionToken"];

        private bool notifyLock = false;
        private DateTime notifyStamp = DateTime.Now;

        public override Task OnConnected()
        {
            MainThread.WriteLine("Websocket Connection Created");
            try
            {
                return base.OnConnected();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            MainThread.WriteLine("Websocket Connection Disconnected");
            try
            {
                return base.OnDisconnected(stopCalled);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override Task OnReconnected()
        {
            MainThread.WriteLine("Websocket Connection Disconnected");
            try
            {
                return base.OnReconnected();
            }
            catch (Exception)
            {
                return null;
            }
        }

        


        public void LoginEvent(string Password)
        {
            try
            {
                var result = WebSocketThread.Users.Login(Password, CurrentToken);
                if (result.Result)
                {

                    Logger.AppendToLog("WebLogin",
                        $"{result.UserObject.Username}:{Context.Request.GetRemoteIpAddress()} has logged in");
                    CurrentUser.LoginEvent("Success", result.UserObject.Token);
                    MainThread.WriteLine($"WebUI Event: {result.UserObject.Username} has logged in");
                }
                else
                {
                    Logger.AppendToLog("WebLogin",
                        $"{Context.Request.GetRemoteIpAddress()} Invalid Login attempt");
                    CurrentUser.LoginEvent("Failure", "");
                }
            }
            catch(Exception)
            {
                MainThread.WriteLine("Unknown error has occured in WebSocket Server.... Restarting", true);
            }
        }

        public void GetStats()
        {
            var a = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            foreach (ServerContainer server in ServerThread.Servers)
            {
                if (server.Name != "")
                {
                    var b = new Dictionary<string, Dictionary<string, string>>();
                    b.Add("Status", new Dictionary<string, string>
                    {
                        {"GameState", server.GameState.ToString()},
                        {"CurrentMap", server.CurrentVariantMap},
                        {"CurrentName", server.CurrentVariantName},
                        {"NextMap", server.NextVariantMap},
                        {"NextName", server.NextVariantName},
                        {"Privacy", server.Privacy.ToString()}
                    });
                    b.Add("Players", new Dictionary<string, string>());
                    foreach (var player in server.CurrentPlayers)
                    {
                        b["Players"].Add(player.Name, $"{player.Score}, {player.Team}");
                    }
                    a.Add(server.Name, b);
                }
            }

            CurrentUser.GetStats(JsonConvert.SerializeObject(a));
        }

        public void GetServerListEvent()
        {
            if (!WebSocketThread.Users.TokenLogin(CurrentToken).Result) return;
            var a = new List<Dictionary<string, string>>();
            foreach (ServerContainer server in ServerThread.Servers)
            {
                if (server.Name != "") //Server is not in a state to send to client skip this pass.
                {
                    var b = new Dictionary<string, string>();
                    b.Add("Index", server.WebGuid.ToString());
                    b.Add("Name", server.Name);
                    b.Add("Instance", server.Instance);
                    a.Add(b);
                }
            }
            CurrentUser.GetServerListEvent(JsonConvert.SerializeObject(a));
        }
        public void GetCurrentPlayersEvent(string serverIndex)
        {
            if (!WebSocketThread.Users.TokenLogin(CurrentToken).Result) return;
                var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                var a = server.CurrentPlayers;
                var b = new List<Dictionary<string, string>>();
                foreach (PlayerContainer playerContainer in a)
                {
                    var c = new Dictionary<string, string>();
                    c.Add("Name", playerContainer.Name);
                    c.Add("Team", playerContainer.Team.ToString());
                    c.Add("Biped", playerContainer.Biped.ToString());
                    c.Add("EmblemURL", playerContainer.EmblemURL);
                    b.Add(c);
                }

                CurrentUser.GetPlayersListEvent(JsonConvert.SerializeObject(b));
            }
        }

        public void KickPlayerEvent(string serverIndex, string PlayerName)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} kicked player {PlayerName}");
                server.ConsoleProxy.KickPlayer(PlayerName);
                CurrentUser.KickPlayerEvent("Success");
            }
        }
        public void SetDescriptionEvent(string serverIndex, string description)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} changed server description to {description}");
                server.Description = description;    
                server.SaveSettings();

            }
        }

        public void AddNewServerMessageEvent(string serverIndex, string message, string duration, string all)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                
                if (all == "true")
                    foreach (ServerContainer o in ServerThread.Servers)
                    {
                        Logger.AppendToLog(o.FileSafeName, $"{TokenResult.UserObject.Username} Added server message {message}");
                        o.serverMessages.AddMessage(message, duration);
                    }
                else
                {
                    Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} Added server message {message}");
                    server.serverMessages.AddMessage(message, duration);
                }
                LoadServerMessagesEvent(serverIndex);
            }
        }

        public void DeleteServerMessageEvent(string serverIndex, string message)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} removed server message {message}");
                server.serverMessages.RemoveMessage(message);
                CurrentUser.DeleteServerMessageEvent();
            }
        }
        public void GetServerStatusEvent(string serverIndex)
        {
            if (!WebSocketThread.Users.TokenLogin(CurrentToken).Result) return;
            var a = new Dictionary<string, string>();
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                //The server will not update strings unless you status/skip
                server.ConsoleProxy.Status();
                a.Add("GameState", server.GameState.ToString());
                a.Add("CurrentMap", server.CurrentVariantMap);
                a.Add("CurrentName", server.CurrentVariantName);
                a.Add("NextMap", server.NextVariantMap);
                a.Add("NextName", server.NextVariantName);
                a.Add("Privacy", server.Privacy.ToString());
                a.Add("ForcedBiped", server.ForcedBiped.ToString());
                a.Add("LobbyRunning", server.LobbyRunning.ToString());
                a.Add("XDelayTimer", server.ForcedXDelayTimer.ToString());
                a.Add("MaxPlayers", server.MaxPlayers.ToString());
                a.Add("BRFix", server.BattleRifleVelocityOverride.ToString());
                a.Add("Description", server.FormattedDescription);
                a.Add("AFKTimer", server.AFKKicktime.ToString());
                a.Add("PCRState", server.PCRState.ToString());
                a.Add("SyncProj", server.ProjectileSync.ToString());
                a.Add("ServerCount", ServerThread.Servers.ValidCount.ToString()); //Hacked to pieces.
                a.Add("CurrentPlaylist", server.CurrentPlaylist);
                CurrentUser.GetServerStatusEvent(JsonConvert.SerializeObject(a));
            }
        }

        public void LoadServerMessagesEvent(string serverIndex)
        {
            if (!WebSocketThread.Users.TokenLogin(CurrentToken).Result) return;
            var a = new List<KeyValuePair<string, string>>();
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                foreach (ServerMessage serverMessage in server.serverMessages)
                    a.Add(new KeyValuePair<string, string>(serverMessage.message, serverMessage.interval.ToString()));
                CurrentUser.LoadServerMessagesEvent(JsonConvert.SerializeObject(a));
            }
        }
        public void SkipServerEvent(string serverIndex)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} skipped match");
                server.ConsoleProxy.Skip();
                CurrentUser.SkipServerEvent("Success");
            }
        }
        public void SetAFKTimerEvent(string serverIndex, string value)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} change the AFK Kick Timer to {value}");
                server.AFKKicktime = int.Parse(value);
                server.SaveSettings();
            }
        }
        public void LoadBanListEvent(string serverIndex)
        {
            if (!WebSocketThread.Users.TokenLogin(CurrentToken).Result) return;
            CurrentUser.LoadBanListEvent(
                JsonConvert.SerializeObject(
                    ServerThread.Servers[Guid.Parse(serverIndex)].GetBannedGamers)
                );
        }

        public void BanPlayerEvent(string serverIndex, string playerName)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)]; if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} banned player {playerName}");
                server.ConsoleProxy.BanPlayer(playerName);
                CurrentUser.BanPlayerEvent("Success");
            }
        }

        public void UnBanPlayerEvent(string serverIndex, string playerName)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} unbanned player {playerName}");
                server.ConsoleProxy.UnBanPlayer(playerName);
                CurrentUser.UnBanPlayerEvent("Success");
            }
        }
        public void TimeoutPlayerEvent(string serverIndex, string playerName)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} timed out player {playerName}");
                server.CurrentPlayers[playerName]?.TimeoutPlayer();
            }
        }
        public void LoadVIPListEvent(string serverIndex)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                CurrentUser.LoadVIPListEvent(
                    JsonConvert.SerializeObject(
                        ServerThread.Servers[Guid.Parse(serverIndex)].GetVIPGamers)
                );
            }
        }

        public void AddVIPPlayerEvent(string serverIndex, string playerName)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} added {playerName} to VIP");
                server.ConsoleProxy.AddVIP(playerName);
                CurrentUser.AddVIPPlayerEvent("success");
            }
        }

        public void RemoveVIPPlayerEvent(string serverIndex, string playerName)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} removed {playerName} from VIP");
                server.ConsoleProxy.RemoveVIP(playerName);
                CurrentUser.RemoveVIPPlayerEvent("Success");
            }
        }
        public void LoadPlaylistsEvent()
        {
            if (!WebSocketThread.Users.TokenLogin(CurrentToken).Result) return;
            var playlists = Directory.GetFiles(ServerThread.PlaylistFolder, "*.hpl");
            List<string> nlists = new List<string>();
            foreach (var playlist in playlists)
                nlists.Add(playlist.Replace(ServerThread.PlaylistFolder, ""));       
            CurrentUser.LoadPlaylistsEvent(JsonConvert.SerializeObject(nlists));
        }

        public void ChangePlaylistEvent(string serverIndex, string playlistname)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName,
                    $"{TokenResult.UserObject.Username} changed playlist to {playlistname}");
                ServerThread.Servers[Guid.Parse(serverIndex)].ConsoleProxy.SetPlaylist(playlistname);
                CurrentUser.ChangePlaylistEvent("Success");
            }
        }

        public void LoadServerLogEvent(string serverIndex)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                var l = JsonConvert.SerializeObject(Logger.GetLog(server.FileSafeName).DumpLogs());
                CurrentUser.LoadServerLogEvent(l);
            }
        }

        public void StopServerEvent(string serverIndex)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName,
                    $"{TokenResult.UserObject.Username} stopped server {server.FormattedName}");
                server.KillServer(false);
                CurrentUser.StopServerEvent("Server has been stopped.");
            }
        }

        public void RestartServerEvent(string serverIndex)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName,
                    $"{TokenResult.UserObject.Username} restarted server {server.FormattedName}");
                server.KillServer();
                CurrentUser.RestartServerEvent("Server has been restarted.");
            }
        }

        public void NotifyServerChangeEvent()
        {
            //Some logic to not spam a bunch of notify events to all clients
            if (!notifyLock)
            {
                Clients.All.NotifyServerChangeEvent();
                notifyStamp = DateTime.Now;
                notifyLock = true;
            }
            else
            {
                if (notifyStamp.AddSeconds(10) < DateTime.Now)
                {
                    notifyLock = false;
                    NotifyServerChangeEvent();
                }
            }
        }

        public void FreezeLobbyEvent(string serverIndex, string state)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName,
                    $"{TokenResult.UserObject.Username} {((state == "true") ? "froze" : "unfroze")} the Lobby.");
                server.LobbyRunning = state != "true";
            }
        }

        public void SetSyncProjEvent(string serverIndex, string state)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName,
                    $"{TokenResult.UserObject.Username} {((state == "true") ? "disabled" : "enabled")} the Projectile Sync.");
                server.ProjectileSync = state == "true";
                server.SaveSettings();
            }
        }
        public void SetPCRStateEvent(string serverIndex, string state)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName,
                    $"{TokenResult.UserObject.Username} {((state == "true") ? "disabled" : "enabled")} the PCR.");
                server.PCRState = state != "true";
                server.SaveSettings();
            }
        }
        public void ForceStartLobbyEvent(string serverIndex)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                Logger.AppendToLog(server.FileSafeName,
                    $"{TokenResult.UserObject.Username} Force stated the lobby.");
                server.ForceStartLobby();
            }
        }

        public void SetPrivacyEvent(string serverIndex, string privacy)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                server.Privacy = (Privacy)Enum.Parse(typeof(Privacy), privacy);
                server.SaveSettings();
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} set privacy to {privacy}");
            }
        }

        public void SetForcedBipedEvent(string serverIndex, string biped)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                server.ForcedBiped = (Biped)Enum.Parse(typeof(Biped), biped);
                server.SaveSettings();
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} set forced biped to {biped}");
            }
        }

        public void SetMaxPlayersEvent(string serverIndex, string playerCount)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                server.MaxPlayers = int.Parse(playerCount);
                server.SaveSettings();
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} set max players to {playerCount}");
            }
        }

        public void SetXDelayTimer(string serverIndex, string xDelayTime)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                server.ForcedXDelayTimer = int.Parse(xDelayTime);
                server.SaveSettings();
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} set xdelay to {xDelayTime}");
            }
        }

        public void SetBRFixEvent(string serverIndex, string value)
        {
            var TokenResult = WebSocketThread.Users.TokenLogin(CurrentToken);
            if (!TokenResult.Result) return;
            var server = ServerThread.Servers[Guid.Parse(serverIndex)];
            if (server == null)
            {
                NotifyServerChangeEvent();
            }
            else
            {
                server.BattleRifleVelocityOverride = float.Parse(value);
                server.SaveSettings();
                Logger.AppendToLog(server.FileSafeName, $"{TokenResult.UserObject.Username} set BRFix to {value}");
            }
        }
        public static void NotifyServerChangeEventEx()
        {
            GlobalHost.ConnectionManager.GetHubContext<ServerHub>().Clients.All.NotifyServerChangeEvent();
        }
    }
}
