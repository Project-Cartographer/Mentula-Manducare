using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MentulaManducare;

namespace mentula_manducare.Objects.Extras
{
    public class ServerDeathRing
    {
        private ServerContainer _server;
        public bool InGameFlop = false;
        public string MapName = "gemini";
        public string VariantName = "Death Ring";
        private int tolerance = 0;
        public ServerDeathRing(ServerContainer baseServer)
        {
            _server = baseServer;
        }

        public void InitDeathRing()
        {
            if (!InGameFlop)
            {
                InGameFlop = true;
                
                    while (!_server.isMapReady)
                    {
                    }

                    var spawnPointCount = _server.ServerMemory.ReadInt(_server.ServerMemory.BlamCachePointer(0x143c100));
                    while (spawnPointCount == 0)
                    {
                        spawnPointCount = _server.ServerMemory.ReadInt(_server.ServerMemory.BlamCachePointer(0x143c100));
                    }

                    var spawnPointReflexOffset =
                        _server.ServerMemory.ReadInt(_server.ServerMemory.BlamCachePointer(0x143c104));
                    var spawnPointReflexStart = _server.ServerMemory.BlamCachePointer(spawnPointReflexOffset);
                    var radius = 4.7f;
                    for (var i = 0; i < spawnPointCount; i++)
                    {
                        //This is a work of art and it worked first try and I know literally 0 about trig...
                        float angle = (float) (i * Math.PI * 2 / spawnPointCount);
                        float x = (float) (Math.Cos(angle) * radius);
                        float y = (float) (6.937 + Math.Sin(angle) * radius) * -1;
                        var itemOffset = i * 52;
                        var itemAddress = spawnPointReflexStart + itemOffset;
                        _server.ServerMemory.WriteFloat(itemAddress, x);
                        _server.ServerMemory.WriteFloat(itemAddress + 4, y);
                        _server.ServerMemory.WriteFloat(itemAddress + 8, 12.3f);
                    }

                    for (int i = 0; i < 16; i++)
                    {
                        var playerObjectIndex = _server.ServerMemory.ReadUShort(0x300026F0 + (i * 0x204));
                        if (playerObjectIndex != ushort.MaxValue)
                        {
                            int playerTableIndexOffset = playerObjectIndex * 12;
                            int playerTableObjectPointerAddress = 0x3003CAE8 + playerTableIndexOffset + 8;
                            int playerTableObjectAddress = _server.ServerMemory.ReadInt(playerTableObjectPointerAddress);
                            _server.ServerMemory.WriteMemory(false, playerTableObjectAddress + 0x208,
                                new byte[] {0x01, 0xFE, 0xFE, 0xFF});
                        }
                    }
            }
        }

        public void IngameTick()
        {
            InitDeathRing();
            for (int i = 0; i < 16; i++)
            {
                var playerObjectIndex = _server.ServerMemory.ReadUShort(0x300026F0 + (i * 0x204));
                if (playerObjectIndex != ushort.MaxValue)
                {
                    int playerTableIndexOffset = playerObjectIndex * 12;
                    int playerTableObjectPointerAddress = 0x3003CAE8 + playerTableIndexOffset + 8;
                    int playerTableObjectAddress = _server.ServerMemory.ReadInt(playerTableObjectPointerAddress);
                    var playerZ = _server.ServerMemory.ReadFloat(playerTableObjectAddress + 0x6C);
                    if (playerZ < 11)
                    {
                        _server.ServerMemory.WriteMemory(false, playerTableObjectAddress + 0x208,
                            new byte[] {0x01, 0xFE, 0xFE, 0xFF});
                    }
                }
            }
        }
    }
}
