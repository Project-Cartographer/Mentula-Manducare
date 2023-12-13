using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using mentula_manducare.Classes;
using mentula_manducare.Enums;
using MentulaManducare;

namespace mentula_manducare.Objects
{
    public class PlayerContainer
    {
        [ScriptIgnore]
        public MemoryHandler Memory;

        public int PlayerIndex;
        private int resolvedStaticIndex = -1;
        private int resolvedDynamicIndex = -1;
        public string validName;
        public PlayerContainer(MemoryHandler Memory, int PlayerIndex)
        {
            this.Memory = Memory;
            this.PlayerIndex = PlayerIndex;
            resolveIndexes();
            validName = Name;
        }
        public bool IsReal => Name != "" && validName == Name;

        public void resolveIndexes()
        {
            var name_ = this.Name;
            if (name_ != StaticName)
            {
                for (var i = 15; i >= 0; i--)
                {
                    if (Memory.ReadStringUnicode(0x530E4C + (i * 0x128), 16, true) == name_)
                        resolvedStaticIndex = i;
                    if (Memory.ReadStringUnicode(0x30002708 + (i * 0x204), 16) == name_)
                        resolvedDynamicIndex = i;
                }
            }
        }
        public string Name
        {
            get => Memory.ReadStringUnicode(0x9917DA + (PlayerIndex * 0x40), 16, true);
            set => Memory.WriteStringUnicode(0x9917DA + (PlayerIndex * 0x40), value, true);
        }

        public string StaticName 
            => Memory.ReadStringUnicode(0x530E4C + (resolvedStaticIndex * 0x128), 16, true);

        public string DynamicName
            => "";
        public Team Team
        {
            get => (Team)Memory.ReadByte(0x530F4C + (resolvedStaticIndex * 0x128), true);
            set => Memory.WriteByte(0x530F4C + (resolvedStaticIndex * 0x128), (byte) value, true);
        }

        public Biped Biped
        {
            get => (Biped) Memory.ReadByte(0x3000274C + (resolvedDynamicIndex * 0x204));
            set => Memory.WriteByte(0x3000274C + (resolvedDynamicIndex * 0x204), (byte) value);
        }

        public byte BipedPrimaryColor
            => Memory.ReadByte(0x530E8C + (resolvedStaticIndex * 0x128), true);

        public byte BipedSecondaryColor
            => Memory.ReadByte(0x530E8D + (resolvedStaticIndex * 0x128), true);

        public byte PrimaryEmblemColor
            => Memory.ReadByte(0x530E8E + (resolvedStaticIndex * 0x128), true);

        public byte SecondaryEmblemColor
            => Memory.ReadByte(0x530E8F + (resolvedStaticIndex * 0x128), true);

        public byte EmblemForeground
            => Memory.ReadByte(0x530E91 + (resolvedStaticIndex * 0x128), true);

        public byte EmblemBackground
            => Memory.ReadByte(0x530E92 + (resolvedStaticIndex * 0x128), true);

        public byte EmblemToggle
            => (byte) (Memory.ReadByte(0x530E93 + (resolvedStaticIndex * 0x128), true) == 0 ? 1 : 0);

        public string EmblemURL =>
            $"http://halo.bungie.net/Stats/emblem.ashx?s=120&0={BipedPrimaryColor.ToString()}&1={BipedSecondaryColor.ToString()}&2={PrimaryEmblemColor.ToString()}&3={SecondaryEmblemColor.ToString()}&fi={EmblemForeground.ToString()}&bi={EmblemBackground.ToString()}&fl={EmblemToggle.ToString()}";

        public float CameraYaw =>
            Memory.ReadFloat(0x53F3A0 + (resolvedStaticIndex * 0x88), true);

        public float CameraPitch =>
            Memory.ReadFloat(0x53F3A4 + (resolvedStaticIndex * 0x88), true);

        public string Place
        {
            //Loop is required because the scoreboard will still keep players who have left in the list.
            get
            {
                for (var i = 0; i < 16; i++)
                {
                    if (StaticName == Memory.ReadStringUnicode(0x9917da + (0x40 * i), 32, true))
                        return i.ToString();
                    return Memory.ReadStringUnicode(0x991BDC + (0x4 * i), 8, true);
                }
                return "Nan";
            }
        }
        public string Score
        {
            //Loop is required because the scoreboard will still keep players who have left in the list.
            get
            {
                for (var i = 0; i < 16; i++)
                {
                    if (StaticName == Memory.ReadStringUnicode(0x9917da + (0x40 * i), 32, true))
                        return Memory.ReadInt(0x991BDC + (0x4 * i), true).ToString();
                }
                return "Nan";
            }
        }

        public Stopwatch LastMovement;
        public bool AFKInit = false;
        public bool IsWarned = false;
        public bool isAFK = false;
        public bool HasMoved = false;
        private float LastCameraYaw = 0;
        private float LastCameraPitch = 0;
        public void TickAFKCheck()
        {
            if (!AFKInit)
            {
                AFKInit = true;
                LastMovement = Stopwatch.StartNew();
                LastCameraPitch = CameraPitch;
                LastCameraYaw = CameraYaw;
            }
            else
            {
                if (LastCameraPitch != CameraPitch | LastCameraYaw != CameraYaw)
                {
                    LastMovement.Restart();
                    LastCameraPitch = CameraPitch;
                    LastCameraYaw = CameraYaw;
                    if (IsWarned)
                        HasMoved = true;
                    isAFK = false;
                    IsWarned = false;
                }
            }
        }



        //Requires more testing, might delete later idk

        public int NetworkIdentifier =>
            Memory.ReadByte(0x530E3C + (resolvedStaticIndex * 0x128), true) - 1;

        public int IPHex =>
            Memory.ReadInt(0x5321DC + (NetworkIdentifier * 0x10C), true);

        public async void TimeoutPlayer()
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < 16; i++)
                {
                    var networkObjectIP = Memory.ReadInt(0x526574 + (i * 0x740), true);
                    if (networkObjectIP != IPHex) continue;
                    var startTime = DateTime.UtcNow;
                    while(DateTime.Now - startTime > TimeSpan.FromSeconds(20))
                        Memory.WriteByte(0x5265CE + (i * 0x740), 0, true);
                    break;
                }
            });
        } 
    }
}
