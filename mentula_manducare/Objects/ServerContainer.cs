using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using mentula_manducare.Classes;
using mentula_manducare.Enums;
using mentula_manducare.Objects.Extras;
using mentula_manducare.Objects.Server;
using MentulaManducare;
using Microsoft.Win32;

namespace mentula_manducare.Objects
{
    public class ServerContainer
    {
        public int Index { get; set; }
        /// <summary>
        /// Property used to insure unique identifiers inside the web UI.
        /// </summary>
        public Guid WebGuid { get; set; }
        public string Instance { get; set; }
        private string _Name = "";
        private bool _xDelayFlop = false;
        private bool _inGameFlop = false;
        private bool _postGameFlop = false;
        private ServerPowerPit powerPit;
        private ServerDeathRing deathRing;
        private ServerAlleyBrawl alleyBrawl;
        public ServerMessageCollection serverMessages;
        public string Name
        {
            get
            {
                if (_Name == "")
                    _Name = ServerMemory.ReadStringUnicode(0x52FC88, 16, true);
                return _Name;
            }
        }

        public bool isLive { get; set; }
        public Process ServerProcess { get; set; }
        public MemoryHandler ServerMemory { get; set; }
        public ConsoleProxy ConsoleProxy { get; set; }
        public ServerPostGameCarnage CarnageReport { get; set; }
        public bool AutoRestart = true;
        private int AFKKicktime_ = 0;
        private int AFKWarntime_ = 0;
        public int AFKKicktime
        {
            get => AFKKicktime_ / 1000;
            set
            {
                AFKKicktime_ = value * 1000;
                AFKWarntime_ = (int)Math.Floor(value * (decimal)0.8) * 1000;
            }
        }
        public string FileSafeName =>
            Instance.Replace(':', '_');

        public SettingsCollection Settings;
        public ServerContainer(Process serverProcess)
        {
            this.ServerProcess = serverProcess;
            ServerMemory = new MemoryHandler(ServerProcess);
            WebGuid = Guid.NewGuid(); //Used to handle the issues with server indexing with detaching/attaching instances.
            GetLaunchParameters(this);
            Settings = new SettingsCollection(FileSafeName);
            serverMessages = new ServerMessageCollection(FileSafeName);
        }

        public void LaunchConsoleProxy()
        {
            ConsoleProxy = new ConsoleProxy(this.Instance);
            LoadSettings();
            CurrentPlayers = new PlayerCollection(this);
            powerPit = new ServerPowerPit(this);
            deathRing = new ServerDeathRing(this);
            alleyBrawl = new ServerAlleyBrawl(this);
            CarnageReport = new ServerPostGameCarnage(this);
        }

        public void KillConsoleProxy()
        {
            ConsoleProxy.Console_.Kill();
        }

        public void KillServer(bool allowrestart = true)
        {
            this.AutoRestart = allowrestart;
            this.ServerProcess.Kill();

        }
        public void Tick()
        {
            foreach (ServerMessage serverMessage in serverMessages)
                if (serverMessage.Tick())
                    ConsoleProxy.SendMessage(serverMessage.message);

            switch (GameState)
            {
                case GameState.Lobby:
                    {
                        if (!LobbyRunning)
                        {
                            RunCountdown =
                                false; //If lobby is frozen keep canceling the count down and set delay timer to absurd value
                            XDelayTimer = ForcedXDelayTimer;
                            RunCountdown = true;
                        }
                        else
                        {
                            if (ForceXDelay & !_xDelayFlop & RunCountdown)
                            {
                                //Fire only after initial countdown starts, reset and change time.
                                RunCountdown = false;
                                XDelayTimer = ForcedXDelayTimer;
                                RunCountdown = true;
                                _xDelayFlop = true;
                            }
                            if (_xDelayFlop & !RunCountdown)
                                _xDelayFlop = false;
                        }

                        _postGameFlop = false;
                        break;
                    }
                case GameState.Starting:
                    break;
                case GameState.InGame:
                    {
                        //Bugfix...
                        RunCountdown = false;

                        if (!_inGameFlop)
                        {
                            while (ServerMemory.ReadUInt(0x4A29BC, true) == 0xFFFFFFFF ||
                                   ServerMemory.ReadInt(0x4A29BC, true) == 0)
                            {
                            }
                            WriteToConsole("Doing Ingame Operations");
                            if (ProjectileSync)
                            {
                                WriteToConsole("Fixing Projectiles");
                                ToggleForcedProjectileSync();
                            }

                            _inGameFlop = true;
                        }
                        //Because of dynamic player table issues instead of processing the whole player collection
                        //just iterate through all possible options, It ain't perfect but it works.
                        if (ForcedBiped != Biped.Disabled)
                            for (var i = 0; i < 16; i++)
                                ServerMemory.WriteByte(0x3000274C + (i * 0x204), (byte)ForcedBiped);

                        if (CurrentVariantName.ToLower().Contains("zombies") || CurrentVariantName.ToLower().Contains("infection"))
                        {
                            if (InternalTimer > 10000)
                            {

                                if (PlayerCount > 1)
                                {
                                    /*
                                     *
                                     * 1: red
                                     * 2: red
                                     * 3: red
                                     * 4: green
                                     * 5: red
                                     */
                                    bool end = true;
                                    foreach (var playerContainer in CurrentPlayers)
                                        if (playerContainer.Team != Team.Green)
                                            end = false;
                                    if (end)
                                        ConsoleProxy.Skip();
                                }

                            }
                        }

                        //EVENTUALLY REMOVABLE
                        if (DoBattleRifleVelocityOverride)
                            BattleRifleVelocity = BattleRifleVelocityOverride;

                        if (AFKKicktime != 0)
                            foreach (PlayerContainer playerContainer in CurrentPlayers)
                            {
                                //VIPList is also the AFK bypass list.
                                if (GetVIPGamers.Contains(playerContainer.Name)) continue;

                                playerContainer.TickAFKCheck();
                                if (playerContainer.HasMoved)
                                {
                                    playerContainer.HasMoved = false;
                                    playerContainer.IsWarned = false;
                                    playerContainer.isAFK = false;
                                    ConsoleProxy.SendMessage(
                                        $"{playerContainer.Name} that was a close one.");
                                }

                                if (playerContainer.LastMovement.ElapsedMilliseconds > AFKWarntime_ &&
                                    !playerContainer.IsWarned)
                                {
                                    playerContainer.IsWarned = true;
                                    ConsoleProxy.SendMessage(
                                        $"{playerContainer.Name} you are about to be kicked for being AFK you should move.");
                                }

                                if (playerContainer.LastMovement.ElapsedMilliseconds > AFKKicktime_ &&
                                    !playerContainer.isAFK)
                                {
                                    playerContainer.isAFK = true;
                                    ConsoleProxy.SendMessage($"{playerContainer.Name} was kicked for being AFK.");
                                    ConsoleProxy.KickPlayer(playerContainer.Name);
                                }
                            }

                        if (CurrentVariantMap == powerPit.MapName && CurrentVariantName.Contains(powerPit.VariantName))
                            powerPit.InGameTick();

                        if (CurrentVariantMap == deathRing.MapName && CurrentVariantName.Contains(deathRing.VariantName))
                            deathRing.IngameTick();

                        if (CurrentVariantMap == alleyBrawl.MapName && CurrentVariantName.Contains(alleyBrawl.VariantName))
                        {
                            alleyBrawl.InGameTick();
                        }

                        break;
                    }
                case GameState.PostGame:
                    {
                        _xDelayFlop = false;
                        //Not sure how but sometimes this bugs causing the game to  get stuck in post game, will remove later just a bugfix fornow
                        XDelayTimer = 0;
                        RunCountdown = false;
                        _inGameFlop = false;
                        powerPit.InGameFlop = false;
                        deathRing.InGameFlop = false;
                        alleyBrawl.InGameFlop = false;
                        if (!_postGameFlop)
                        {
                            InternalTimer = 0;
                            if (AFKKicktime != 0)
                            {
                                foreach (PlayerContainer playerContainer in CurrentPlayers)
                                    playerContainer.AFKInit = false;

                            }

                            if (!CurrentVariantName.ToLower().Contains("zombies"))
                                SendStats();

                            _postGameFlop = true;
                        }

                        break;
                    }
                case GameState.MatchMaking:
                    break;
                case GameState.Unknown:
                    break;
                default:
                    {
                        WriteToConsole(
                            $"What in the absolute tom fuckery is going on, reached a Gamestate that doesn't exist.");
                        break;
                    }
            }
        }
        public void WriteToConsole(string message) =>
            MainThread.WriteLine($"[{Name}]: {message}");

        public void SaveSettings()
        {
            Settings.AddSetting("XDelayTimer", ForcedXDelayTimer.ToString());
            Settings.AddSetting("ForcedBiped", ForcedBiped.ToString());
            Settings.AddSetting("Privacy", Privacy.ToString());
            Settings.AddSetting("MaxPlayers", MaxPlayers.ToString());
            Settings.AddSetting("BRFix", BattleRifleVelocityOverride.ToString());
            Settings.AddSetting("Description", FormattedDescription);
            Settings.AddSetting("AFKTimer", AFKKicktime.ToString());
            Settings.AddSetting("PCRDisable", PCRState.ToString());
            Settings.AddSetting("SyncProj", ProjectileSync.ToString());
        }

        //This is absolute dog shit however it is just for testing purposes.
        public async void SendStats()
        {
            //Store the carnage report to a file
            WriteToConsole("Uploading stats");
            try
            {
                var jsonname = CarnageReport.SaveJSON();
                //Create a get request to check if the playlist exists in the db
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://www.halo2pc.com/test-pages/CartoStat/API/get.php?Type=PlaylistCheck&Playlist_Checksum=" + CarnageReport.PlaylistChecksum);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    //If the playlist doesn't exist (status code 201) upload it to the server
                    if ((int)response.StatusCode == 201)
                    {
                        //Create a post request sending the hpl file
                        HttpClient httpClient = new HttpClient();
                        MultipartFormDataContent form = new MultipartFormDataContent();
                        form.Add(new StringContent("PlaylistUpload"), "Type");
                        form.Add(new StringContent(CarnageReport.PlaylistChecksum), "Playlist_Checksum");
                        var fileStream = new FileStream(CarnageReport.PlaylistFile, FileMode.Open);
                        form.Add(new StreamContent(fileStream), "file", CarnageReport.PlaylistFile.Split('\\').Last());
                        HttpResponseMessage presponse = await httpClient.PostAsync("https://www.halo2pc.com/test-pages/CartoStat/API/post.php", form);
                        WriteToConsole("Playlist uploaded and verified");
                        if ((int)presponse.StatusCode != 200)
                            throw new Exception("Fuck");
                        httpClient.Dispose();
                    }
                    else if ((int)response.StatusCode == 500)
                    {
                        throw new Exception("Fuck 2");
                    }
                    //Create a post request sending the stats json
                    HttpClient httpClienta = new HttpClient();
                    MultipartFormDataContent forma = new MultipartFormDataContent();
                    forma.Add(new StringContent("GameStats"), "Type");
                    var fileStreama = new FileStream(jsonname, FileMode.Open);
                    forma.Add(new StreamContent(fileStreama), "file", jsonname);
                    HttpResponseMessage responsea = await httpClienta.PostAsync("https://www.halo2pc.com/test-pages/CartoStat/API/post.php", forma);
                    WriteToConsole("Stats uploaded.");
                    if ((int)responsea.StatusCode != 200)
                        throw new Exception("Fuck 3");
                    httpClienta.Dispose();
                }
            } catch(Exception e)
            {
                WriteToConsole("Uploading stats failed.");
                Logger.AppendToLog("ErrorLog", e.Message + e.StackTrace);
            }
        }
        public void LoadSettings()
        {
            ForcedXDelayTimer = int.Parse(Settings.GetSetting("XDelayTimer", ForcedXDelayTimer.ToString()));
            ForcedBiped = (Biped)Enum.Parse(typeof(Biped), Settings.GetSetting("ForcedBiped", ForcedBiped.ToString()));
            Privacy = (Privacy)Enum.Parse(typeof(Privacy), Settings.GetSetting("Privacy", Privacy.ToString()));
            MaxPlayers = int.Parse(Settings.GetSetting("MaxPlayers", MaxPlayers.ToString()));
            BattleRifleVelocityOverride = float.Parse(Settings.GetSetting("BRFix", BattleRifleVelocityOverride.ToString()));
            Description = Settings.GetSetting("Description", "");
            AFKKicktime = int.Parse(Settings.GetSetting("AFKTimer", "0"));
            PCRState = bool.Parse(Settings.GetSetting("PCRDisable", "true"));
            ProjectileSync = bool.Parse(Settings.GetSetting("SyncProj", "false"));
        }
        public string FormattedName =>
            $"{Index} - {Name}:{Instance}";

        [ScriptIgnore] public PlayerCollection CurrentPlayers;

        public ulong XUID =>
            ServerMemory.ReadULong(0x52FC50, true);

        public GameState GameState =>
            (GameState)ServerMemory.ReadByte(0x3C40AC, true);

        public int PlayerCount =>
            ServerMemory.ReadByte(0x53329C, true);

        public string CurrentPlaylist =>
            ServerMemory.ReadStringUnicode(0x3B3704 + (ServerThread.PlaylistFolder.Length * 2), 100, true);

        private string _ServiceName = "";

        public string ServiceName
        {
            get
            {
                if (_ServiceName == "")
                    _ServiceName = ServerMemory.ReadStringUnicode(0x3B4AC4, 50, true);
                return _ServiceName;
            }
        }

        /// <summary>
        /// This value is the internal clock that does not reflect match time or any other visibly trackable time, it will reset everytime there is a blue screen.
        /// It only functions during the InGame GameState.
        /// </summary>
        public uint InternalTimer
        {
            get => ServerMemory.ReadUInt(0x3000257C);
            set => ServerMemory.WriteUInt(0x3000257C, value);
        }
        public int MaxPlayers
        {
            get => ServerMemory.ReadByte(0x534858, true);
            set => ConsoleProxy.Players(value);
        }

        public string RegistryPath =>
            $"SYSTEM\\ControlSet001\\Services\\{ServiceName}\\";

        public string BanRegistry =>
            $"{RegistryPath}\\Parameters\\LIVE\\Banned Gamers";

        public string VIPRegistry =>
            $"{RegistryPath}\\Parameters\\LIVE\\VIPs";

        public string CurrentVariantMap =>
            ServerMemory.ReadStringUnicode(0x534894, 200, true).Split('\\').Last();


        public string CurrentVariantName =>
            ServerMemory.ReadStringUnicode(0x534A18, 100, true);

        //The next three properties are in fact the gayest things I have ever had to figure out.

        public int NextVariantIndex =>
            ServerMemory.ReadInt(0x48D6EC, true);

        public string NextVariantMap =>
            ServerMemory.ReadStringAscii(0x450854 + (NextVariantIndex * 0x21C), 200, true).Split('\\').Last();

        public string NextVariantName =>
            ServerMemory.ReadStringUnicode(0x4506C4 + (NextVariantIndex * 0x21C), 200, true);

        public Privacy Privacy
        {
            get => (Privacy)ServerMemory.ReadByte(0x534850, true);
            set
            {
                if (value == Privacy.Closed)
                    ServerMemory.WriteByte(0x534850, (byte)Privacy.Closed, true);
                else
                    ConsoleProxy.Privacy(value);
            }
        }

        public List<string> GetBannedGamers =>
            Registry.LocalMachine.OpenSubKey(BanRegistry)?.GetValueNames().ToList();

        public List<string> GetVIPGamers =>
            Registry.LocalMachine.OpenSubKey(VIPRegistry)?.GetValueNames().ToList();

        public Biped ForcedBiped = Biped.Disabled;

        public bool ForceXDelay => ForcedXDelayTimer != 10; //If XDelayTimer is not base value return true.
        public int ForcedXDelayTimer = 10; //10 is base XDelay timing.



        //Look at these dirty whores below
        public int XDelayTimer
        {
            get => ServerMemory.ReadInt(0x534870, true);
            set => ServerMemory.WriteInt(0x534870, value, true);
        }

        public bool RunCountdown
        {
            get => ServerMemory.ReadBool(0x53486C, true);
            set => ServerMemory.WriteBool(0x53486C, value, true);
        }

        public bool LobbyRunning = true;


        public void ForceStartLobby()
        {
            RunCountdown = true;
            XDelayTimer = 0;
        }

        //All of this Battle rifle shit will eventually be removable.
        public bool DoBattleRifleVelocityOverride => BattleRifleVelocityOverride != 1200f;
        public float BattleRifleVelocityOverride = 1200f;

        public float BattleRifleVelocity
        {
            set
            {
                ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0xA4EC88), value);
                ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0xA4EC8C), value);
            }
        }


        private byte[] description_;
        public string FormattedDescription { get; set; }
        public string Description
        {
            get { return Encoding.Unicode.GetString(description_); }
            set
            {
                FormattedDescription = value;
                if(FormattedDescription.Length > 31)
                    FormattedDescription = FormattedDescription.Substring(0, 31);
                var bytes = new List<byte>();
                var strings = ServerContainer.SymbolsSpecifier.Split(value);
                var charArray = new[] { '[', ']' };
                foreach (var string_ in strings)
                {
                    if (SymbolsSpecifier.Match(string_).Success)
                    {
                        bytes.Add(byte.Parse(string_.Trim(charArray).Substring(0, 2), NumberStyles.AllowHexSpecifier));
                        bytes.Add(byte.Parse(string_.Trim(charArray).Substring(2, 2), NumberStyles.AllowHexSpecifier));
                    }
                    else
                        bytes.AddRange(Encoding.Unicode.GetBytes(string_));
                }
                bytes.Add(00);
                bytes.Add(00);
                description_ = bytes.ToArray();
                ServerMemory.WriteMemory(true, 0x5347F8, bytes.ToArray());
            }
        }

        private bool PCRState_ = true;
        public bool PCRState
        {
            get => PCRState_;
            set
            {
                TogglePCR(value);
                PCRState_ = value;
            }
        }
        private void TogglePCR(bool state)
        {
            if (state)
            {
                ServerMemory.WriteMemory(true, 0xE579, new byte[] { 0x74, 0x10 });
                ServerMemory.WriteMemory(true, 0xE58B, new byte[] { 0xB8, 0x0A, 0x0, 0x0, 0x0 });
                ServerMemory.WriteMemory(true, 0xE590, new byte[] { 0x83, 0xC0, 0x19 });
            }
            else
            {
                ServerMemory.WriteMemory(true, 0xE579, new byte[] { 0xEB, 0x10 });
                ServerMemory.WriteMemory(true, 0xE58B, new byte[] { 0xB8, 0x05, 0x0, 0x0, 0x0 });
                ServerMemory.WriteMemory(true, 0xE590, new byte[] { 0x83, 0xC0, 0x0 });
            }
        }

        private bool ProjectileSync_ = false;

        public bool ProjectileSync
        {
            get => ProjectileSync_;
            set
            {
                //ToggleForcedProjectileSync(value);
                ProjectileSync_ = value;
            }
        }

        public bool isMapReady =>
            ServerMemory.ReadStringAscii(ServerMemory.ReadInt(ServerMemory.ReadInt(0x4A29BC, true) + 8) + 0x27100, 4) == "gtam" && InternalTimer > 200;
        private void ToggleForcedProjectileSync()
        {
            /*
             Old
                Forces the server to spawn it's own projectiles instead of a clients.
                weapon_fire+162    1E28                movsx   eax, word ptr [esi+52]
                weapon_fire+166    1E28                sub     eax, 1 <--- Changes this
                weapon_fire+169    1E28                jz      short loc_95C80D
                weapon_fire+16B    1E28                sub     eax, 1 1 <--- Changes this
                weapon_fire+16E    1E28                mov     [esp+1E28h+Barrel_Prediction_Not_Continuous], 1
                weapon_fire+173    1E28                jz      short loc_95C80D
                weapon_fire+175    1E28                mov     [esp+1E28h+Barrel_Prediction_None], 1
            
            //if (state)
            //{
            //    //sub eax, 00
            //    ServerMemory.WriteMemory(true, 0x140AAD, new byte[] { 0x83, 0xE8, 0x00 });
            //    ServerMemory.WriteMemory(true, 0x140AB2, new byte[] { 0x83, 0xE8, 0x00 });
            //}
            //else
            //{
            //    //sub eax, 01
            //    ServerMemory.WriteMemory(true, 0x140AAD, new byte[] { 0x83, 0xE8, 0x01 });
            //    ServerMemory.WriteMemory(true, 0x140AB2, new byte[] { 0x83, 0xE8, 0x01 });
            //}
             */

            while (!isMapReady)
            { }
            var TagTableStart = ServerMemory.ReadInt(ServerMemory.ReadInt(0x4A29BC, true) + 8) + 0x27100;

            //Unset meme tag changes because super wanted to try it without them globally enabled.
            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0xA84DD4), 400);
            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0xA84DD8), 400);

            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0xB7F914), 400);
            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0xB7F918), 400);

            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0xCE4598), 1200);
            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0xCE459C), 1200);

            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0x81113C), 1200);
            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0x811140), 1200);

            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0x97A194), 400);
            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0x97A198), 400);

            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0x7E7E20), 800);
            ServerMemory.WriteFloat(ServerMemory.BlamCachePointer(0x7E7E24), 800);

            //Loop through all possible tag entries, can't use Tag Count because MemeGun broke that to always just say max count
            for (int i = 0; i < 15064; i++)
            {
                //Current Tag Index Entry Address
                var cAddress = TagTableStart + (i * 16);
                //Current Tag Index Entry Identifier
                var cIdent = ServerMemory.ReadUInt(cAddress + 4);
                //If the Identifier is valid proceed
                if (cIdent != 0xFFFFFFFF && cIdent != 0x0)
                {
                    //Current Tag Index Class
                    var cClass = ServerMemory.ReadStringAscii(cAddress, 4);
                    if (cClass == "paew") //Set Weapon Prediction Types to None
                    {
                        var cTagAddress = ServerMemory.BlamCachePointer(ServerMemory.ReadInt(cAddress + 8));
                        int BarrelCount = ServerMemory.ReadInt(cTagAddress + 0x2D0);
                        int BarrelAddress = ServerMemory.BlamCachePointer(ServerMemory.ReadInt(cTagAddress + 0x2D4));
                        for (int j = 0; j < BarrelCount; j++) //Loop through all avaliable barrels
                        {
                            var CurrentBarrel = BarrelAddress + (j * 0xEC);
                            if (ServerMemory.ReadShort(CurrentBarrel + 52) != 0)
                            { //Check only here for when testing Continuous mode
                                ServerMemory.WriteShort(CurrentBarrel + 52, 0); //Set to None change to 1 to go to Continuous
                            }
                            else
                            {
                                Logger.AppendToLog("WeaponAddresses", $"{(ServerMemory.ReadInt(cTagAddress + 0x2D4) + (j * 0xEC) + 52):X}");
                            }
                        }
                    }

                    if (cClass == "jorp") //Unset Projectile Travels Instantly flag
                    {
                        var cTagAddress = ServerMemory.BlamCachePointer(ServerMemory.ReadInt(cAddress + 8));
                        int cFlags = ServerMemory.ReadInt(cTagAddress + 0xBC);
                        if (cFlags != 0)
                            cFlags &= ~32;
                        //Bitmask out the flag for Projectile Travels Instantly helps some projectiles sync properly faster
                        ServerMemory.WriteInt(cTagAddress + 0xBC, cFlags);
                    }
                }
            }
        }
        /* ================================ *
         *  Static Properties and methods   *
         * ================================ */


        public static Regex ServerInstanceMatch = new Regex(@"-instance:[(\d|\w)]+");
        public static Regex ServerLiveMatch = new Regex(@"-live+");
        public static Regex SymbolsSpecifier = new Regex("(\\[(?:[0-9A-Fa-f]{4}|0-9A-Fa-f]{6})\\])");
        public static void GetLaunchParameters(ServerContainer container)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + container.ServerProcess.Id))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                var CommandLine = objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                container.Instance = ServerInstanceMatch.Matches(CommandLine)[0]?.Value;
                container.isLive = ServerLiveMatch.IsMatch(CommandLine);
            }
        }

        private static Random random = new Random();
        //TODO: Refactor this into it's own class when structure of plugin's is created.
        public static void FillPoints(ref List<PointF> Points, int count, float YMin, float YMax, float XMin, float XMax)
        {
            for (int i = 0; i < count; i++)
            {
                float Y = (float)((YMax - YMin) * random.NextDouble() + YMin);
                float X = (float)((XMax - XMin) * random.NextDouble() + XMin);
                Points.Add(new PointF(X, Y));
            }
        }
    }
}
