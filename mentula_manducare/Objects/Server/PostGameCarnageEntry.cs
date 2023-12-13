using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using mentula_manducare.Classes;

namespace mentula_manducare.Objects.Server
{
    public class PostGameCarnageEntry
    {
        
        [ScriptIgnore]
        private MemoryHandler _memory;
        [ScriptIgnore]
        private byte _index;
        [ScriptIgnore]
        private static int baseOffset = 0x4DC722;
        [ScriptIgnore]
        private static int rtPCROffset = 0x4DD1EE;
        [ScriptIgnore]
        private static int PCROffset = 0x49F6B0;
        [ScriptIgnore]
        private int calcRTPCROffset;
        [ScriptIgnore]
        private int calcBaseOffset;
        [ScriptIgnore]
        private int calcPCROffset;
        public int EndGameIndex;
        public PostGameCarnageEntry(MemoryHandler memory, byte index)
        {
            this._memory = memory;
            this._index = index;
            this.calcBaseOffset = baseOffset + (0x94 * index);
            this.calcRTPCROffset = rtPCROffset + (0x36A * index);
            var tName = Gamertag;
            for (var i = 0; i < 16; i++)
                if (tName == _memory.ReadStringUnicode(PCROffset + (i * 0x110), 16, true))
                {
                    calcPCROffset = PCROffset + (i * 0x110);
                    EndGameIndex = i;
                }
        }
        #region BasicData
        public ulong XUID =>
            _memory.ReadULong(calcBaseOffset, true);
        public string Gamertag =>
            _memory.ReadStringUnicode(calcBaseOffset + 0xA,16, true);
        public byte PrimaryColor =>
            _memory.ReadByte(calcBaseOffset + 0x4A, true);
        public byte SecondaryColor =>
            _memory.ReadByte(calcBaseOffset + 0x4B, true);
        public byte PrimaryEmblem =>
            _memory.ReadByte(calcBaseOffset + 0x4C, true);
        public byte SecondaryEmblem =>
            _memory.ReadByte(calcBaseOffset + 0x4D, true);
        public byte PlayerModel =>
            _memory.ReadByte(calcBaseOffset + 0x4E, true);
        public byte EmblemForeground =>
            _memory.ReadByte(calcBaseOffset + 0x4F, true);
        public byte EmblemBackground =>
            _memory.ReadByte(calcBaseOffset + 0x50, true);
        public byte EmblemToggle =>
            _memory.ReadByte(calcBaseOffset + 0x51, true);
        public string ClanDescription =>
            _memory.ReadStringUnicode(calcBaseOffset + 0x5A, 16, true);
        public string ClanTag =>
            _memory.ReadStringUnicode(calcBaseOffset + 0x7A, 6, true);
        public byte Team =>
            _memory.ReadByte(calcBaseOffset + 0x86, true);
        public byte Handicap =>
            _memory.ReadByte(calcBaseOffset + 0x87, true);
        public byte Rank =>
            _memory.ReadByte(calcBaseOffset + 0x88, true);
        public byte Nameplate =>
            _memory.ReadByte(calcBaseOffset + 0x8B, true);

        #endregion
        #region GameData

        public string Place =>
            _memory.ReadStringUnicode(calcPCROffset + 0xE0, 16, true);

        public string Score =>
            _memory.ReadStringUnicode(calcPCROffset + 0x40, 16, true);

        public ushort Kills =>
            _memory.ReadUShort(calcRTPCROffset, true);

        public ushort Assists =>
            _memory.ReadUShort(calcRTPCROffset + 0x2, true);

        public ushort Deaths =>
            _memory.ReadUShort(calcRTPCROffset + 0x4, true);

        public ushort Betrayals =>
            _memory.ReadUShort(calcRTPCROffset + 0x6, true);

        public ushort Suicides =>
            _memory.ReadUShort(calcRTPCROffset + 0x8, true);

        public ushort BestSpree =>
            _memory.ReadUShort(calcRTPCROffset + 0xA, true);

        public ushort TimeAlive =>
            _memory.ReadUShort(calcRTPCROffset + 0xC, true);

        public ushort ShotsFired =>
            _memory.ReadUShort(calcPCROffset + 0x84, true);

        public ushort ShotsHit =>
            _memory.ReadUShort(calcPCROffset + 0x88, true);

        public ushort HeadShots =>
            _memory.ReadUShort(calcPCROffset + 0x8C, true);

        //CTF
        public ushort FlagScores =>
            _memory.ReadUShort(calcRTPCROffset + 0xE, true);

        public ushort FlagSteals =>
            _memory.ReadUShort(calcRTPCROffset + 0x10, true);

        public ushort FlagSaves =>
            _memory.ReadUShort(calcRTPCROffset + 0x12, true);

        public ushort FlagUnk =>
            _memory.ReadUShort(calcRTPCROffset + 0x14, true);
        //Assault
        public ushort BombScores =>
            _memory.ReadUShort(calcRTPCROffset + 0x18, true);

        public ushort BombKills =>
            _memory.ReadUShort(calcRTPCROffset + 0x1A, true);

        public ushort BombGrabs =>
            _memory.ReadUShort(calcRTPCROffset + 0x1C, true);

        //Oddball
        public ushort BallScore =>
            _memory.ReadUShort(calcRTPCROffset + 0x20, true);

        public ushort BallKills =>
            _memory.ReadUShort(calcRTPCROffset + 0x22, true);

        public ushort BallCarrierKills =>
            _memory.ReadUShort(calcRTPCROffset + 0x24, true);
        //KotH
        public ushort KingKillsAsKing =>
            _memory.ReadUShort(calcRTPCROffset + 0x26, true);

        public ushort KingKilledKings =>
            _memory.ReadUShort(calcRTPCROffset + 0x28, true);

       //Juggernaut
       public ushort JuggKilledJuggs =>
           _memory.ReadUShort(calcRTPCROffset + 0x3C, true);

       public ushort JuggKillsAsJugg =>
           _memory.ReadUShort(calcRTPCROffset + 0x3E, true);

       public ushort JuggTime =>
           _memory.ReadUShort(calcRTPCROffset + 0x40, true);
       //Territories
       public ushort TerrTaken =>
           _memory.ReadUShort(calcRTPCROffset + 0x46, true);

       public ushort TerrLost =>
           _memory.ReadUShort(calcRTPCROffset + 0x48, true);

       #endregion

       public ushort[] MedalData
       {
           get
           {
               var arr = new ushort[24];
               for (var i = 0; i < 24; i++)
                   arr[i] = _memory.ReadUShort(calcRTPCROffset + 0x4C + (i * 2), true);
               return arr;
           }
       }

       public List<ushort[]> WeaponData
       {
           get
           {
               var list = new List<ushort[]>();
               for (var i = 0; i < 36; i++)
               {
                   var arr = new ushort[6];
                   for (var j = 0; j < 6; j++)
                       arr[j] = _memory.ReadUShort(calcRTPCROffset + 0xF0 + (i * 0x10) + (j * 2), true);
                   list.Add(arr);
               }

               return list;
           }
       }

        [ScriptIgnore]
        private List<int[]> versusData;
        public List<int[]> VersusData
        {
            get
            {
                if (versusData == null)
                {
                    var list = new List<int[]>();
                    for (var i = 0; i < 16; i++)
                        list.Add(new[] {_memory.ReadInt(calcPCROffset + 0x90 + (i * 0x4), true), 0});
                    versusData = list;
                }

                return versusData;
            }
            set => versusData = value;
        }
    }
}
