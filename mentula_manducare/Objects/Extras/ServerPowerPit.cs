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
    public class ServerPowerPit
    {
        private ServerContainer _server;
        public bool InGameFlop = false;
        public string MapName = "headlong";
        public string VariantName = "Power Pit";
        public ServerPowerPit(ServerContainer baseServer)
        {
            _server = baseServer;
        }

        public void InitPowerPit()
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
                    MainThread.WriteLine(spawnPointCount);
                    List<PointF> aPoints = new List<PointF>();
                    List<PointF> bPoints = new List<PointF>();
                    List<PointF> cPoints = new List<PointF>();
                    List<PointF> dPoints = new List<PointF>();

                    ServerContainer.FillPoints(ref aPoints, 45, -4.4f, -1.6f, 18.3f, 18.9f);
                    ServerContainer.FillPoints(ref bPoints, 45, -5.0f, -4.46f, 18.3f, 16.4f);
                    ServerContainer.FillPoints(ref cPoints, 45, -4.5f, -1.6f, 15.7f, 16.4f);
                    ServerContainer.FillPoints(ref dPoints, 48, -1.63f, -1f, 16.3f, 18.2f);
                    List<PointF> points = new List<PointF>();
                    points.AddRange(aPoints);
                    points.AddRange(bPoints);
                    points.AddRange(cPoints);
                    points.AddRange(dPoints);
                    for (var i = 0; i < spawnPointCount; i++)
                    {
                        var itemOffset = i * 52;
                        var itemAddress = spawnPointReflexStart + itemOffset;
                        _server.ServerMemory.WriteFloat(itemAddress, points[i].X);
                        _server.ServerMemory.WriteFloat(itemAddress + 4, points[i].Y);
                        _server.ServerMemory.WriteFloat(itemAddress + 8, 20f);
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
        public void InGameTick()
        {
            InitPowerPit();
        }
    }
}
