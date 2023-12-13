using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MentulaManducare;

namespace mentula_manducare.Objects.Server
{
    public class ServerPostGameCarnage
    {
        [ScriptIgnore]
        private ServerContainer Server_;
        [ScriptIgnore]
        private static string BasePath = $"{MainThread.BasePath}\\Stats";



        

        public ServerPostGameCarnage(ServerContainer Server)
        {
            this.Server_ = Server;
            this.Variant = new VariantM(Server);
        }
        [ScriptIgnore]
        public string PlaylistFile =>
            Server_.ServerMemory.ReadStringUnicode(0x46ECC4, 600, true);
        public string PlaylistChecksum
        {
            get
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(PlaylistFile))
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
        }

        public string Scenario =>
            Server_.ServerMemory.ReadStringUnicode(0x4DC504, 200, true).Split('\\').Last();

        public VariantM Variant;
        public Dictionary<string, string> Server => new Dictionary<string, string>
        {
            {"XUID", Server_.XUID.ToString() },
            {"Name", Server_.Name }
        };

        public List<PostGameCarnageEntry> Players
        {
            get
            {
                var list = new List<PostGameCarnageEntry>();
                for (byte index = 0; index < 16; index++)
                {
                    var newEntry = new PostGameCarnageEntry(Server_.ServerMemory, index);
                    if (newEntry.XUID != 0 && newEntry.Gamertag != "")
                        list.Add(newEntry);
                }

                //Shrink Versus Table to the size of players
                foreach (var t in list)
                    t.VersusData.RemoveRange(list.Count, (16 - list.Count));

                //Populate Versus Data
                for (var i = 0; i < list.Count; i++)
                {
                    for (var j = 0; j < list.Count; j++)
                    {
                        list[i].VersusData[list[j].EndGameIndex][1] = list[j].VersusData[list[i].EndGameIndex][0];
                    }
                }

                return list;
            }
        }
        public string SaveJSON()
        {
            string f = $"{BasePath}\\{Server_.FileSafeName}_{DateTime.Now.ToFileTimeUtc()}.json";
            StreamWriter fs = new StreamWriter(File.Create(f));
            fs.Write(new JavaScriptSerializer().Serialize(this));
            fs.Flush();
            fs.Close();
            fs.Dispose();
            return f;
        }
    }

    public class VariantM
    {
        [ScriptIgnore]
        private ServerContainer Server;
        public VariantM(ServerContainer Server)
        {
            this.Server = Server;
        }
        public string Name =>
            Server.ServerMemory.ReadStringUnicode(0x4DC3D4, 16, true);
        public byte Type =>
            Server.ServerMemory.ReadByte(0x4DC414, true);

        public Dictionary<string, string> Settings => new Dictionary<string, string>
        {
            {"Team Play", Server.ServerMemory.ReadByte(0x54F7E4, true).ToString()}
        };
    }
}
