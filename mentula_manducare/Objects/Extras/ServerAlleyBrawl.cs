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
    public class ServerAlleyBrawl
    {
        private ServerContainer _server;
        public bool InGameFlop = false;
        public bool initFlop = false;
        public string MapName = "street_sweeper";
        public string VariantName = "Alley Brawl";
        public float Xmin = 71.19314f;
        public float Xmax = 73.79544f;

        public float Ymin = 1.087251f;
        public float Ymax = 1.873158f;

        public float Zmin = 2f;
        public float Zmax = 4f;

        private List<PointF> points = new List<PointF>();
        public ServerAlleyBrawl(ServerContainer baseServer)
        {
            _server = baseServer;
            List<PointF> aPoints = new List<PointF>();
            List<PointF> bPoints = new List<PointF>();
            //Two Boxes
            ServerContainer.FillPoints(ref aPoints, 68, 11.49f, 18.89f, 51f, 58f);
            ServerContainer.FillPoints(ref bPoints, 69, 11.18f, 19.18f, 95.1f, 99.33f);
            points.AddRange(aPoints);
            points.AddRange(bPoints);

            //Main Side
            // ServerContainer.FillPoints(ref aPoints, 68 + 69, 11.49f, 18.89f, 51f, 99.33f);
            // points.AddRange(aPoints);
            //All Sides
            //ServerContainer.FillPoints(ref aPoints, 68, 11.49f, 18.89f, 51f, 99.33f);
            //ServerContainer.FillPoints(ref bPoints, 69, -8.8f, 0.4f, 61f, 104f);
            //points.AddRange(aPoints);
            //points.AddRange(bPoints);
        }

        public void InitAlleyBrawl()
        {
            if (!InGameFlop)
            {
                //TODO: Add in Objective object movement. CTF, Oddball, KOTH Add actual teleporters?
                initFlop = false;
                InGameFlop = true;
                while (!_server.isMapReady)
                {

                }
                var spawnPointCount = _server.ServerMemory.ReadInt(_server.ServerMemory.BlamCachePointer(0x143c100));
                //In case the map hasn't fully loaded for some reason wait till it does.
                while (spawnPointCount == 0)
                    spawnPointCount = _server.ServerMemory.ReadInt(_server.ServerMemory.BlamCachePointer(0x143c100));

                var spawnPointReflexOffset =
                    _server.ServerMemory.ReadInt(_server.ServerMemory.BlamCachePointer(0x143c104));
                var spawnPointReflexStart = _server.ServerMemory.BlamCachePointer(spawnPointReflexOffset);
                for (var i = 0; i < spawnPointCount; i++)
                {
                    var itemOffset = i * 52;
                    var itemAddress = spawnPointReflexStart + itemOffset;
                    _server.ServerMemory.WriteFloat(itemAddress, points[i].X);
                    _server.ServerMemory.WriteFloat(itemAddress + 4, points[i].Y);
                    _server.ServerMemory.WriteFloat(itemAddress + 8, 7.87f);
                }

                //Move Objective locations.

                #region CTF
                var redSpawn = _server.ServerMemory.BlamCachePointer(0x143f92c);
                _server.ServerMemory.WriteFloat(redSpawn, 51.65494919f); //X
                _server.ServerMemory.WriteFloat(redSpawn + 0x4, 14.88004017f); //Y
                _server.ServerMemory.WriteFloat(redSpawn + 0x8, 2.506720304f); //z
                var redReturn = _server.ServerMemory.BlamCachePointer(0x143f94c);
                _server.ServerMemory.WriteFloat(redReturn, 51.65494919f); //X
                _server.ServerMemory.WriteFloat(redReturn + 0x4, 14.88004017f); //Y
                _server.ServerMemory.WriteFloat(redReturn + 0x8, 2.506720304f); //z
                var blueSpawn = _server.ServerMemory.BlamCachePointer(0x143f8ec);
                _server.ServerMemory.WriteFloat(blueSpawn, 98.40775299f); //X
                _server.ServerMemory.WriteFloat(blueSpawn + 0x4, 14.88004017f); //Y
                _server.ServerMemory.WriteFloat(blueSpawn + 0x8, 2.506720304f); //z
                var blueReturn = _server.ServerMemory.BlamCachePointer(0x143f90c);
                _server.ServerMemory.WriteFloat(blueReturn, 98.40775299f); //X
                _server.ServerMemory.WriteFloat(blueReturn + 0x4, 14.88004017f); //Y
                _server.ServerMemory.WriteFloat(blueReturn + 0x8, 2.506720304f); //z
                #endregion

                #region Oddball

                var oddBallSpawn = _server.ServerMemory.BlamCachePointer(0x144012c);
                for (var i = 0; i < 10; i++)
                {
                    var offset = oddBallSpawn + (i * 32);
                    _server.ServerMemory.WriteFloat(offset, 72.95675f);
                    _server.ServerMemory.WriteFloat(offset + 4, 10.55947f);
                    _server.ServerMemory.WriteFloat(offset + 8, 20.36735f);
                }
                #endregion


                for (int i = 0; i < 16; i++)
                {
                    var playerObjectIndex = _server.ServerMemory.ReadUShort(0x300026F0 + (i * 0x204));
                    if (playerObjectIndex != ushort.MaxValue)
                    {
                        int playerTableIndexOffset = playerObjectIndex * 12;
                        int playerTableObjectPointerAddress = 0x3003CAE8 + playerTableIndexOffset + 8;
                        int playerTableObjectAddress =
                            _server.ServerMemory.ReadInt(playerTableObjectPointerAddress);
                        //_server.ServerMemory.WriteMemory(false, playerTableObjectAddress + 0x208,
                        //    new byte[] {0x01, 0xFE, 0xFE, 0xFF});
                    }
                }

                initFlop = true;

            }
        }

        public async void InGameTick()
        {
            InitAlleyBrawl();
            if (initFlop)
            {
                var a = 0;
                for (int i = 0; i < 16; i++)
                {
                    var playerObjectIndex = _server.ServerMemory.ReadUShort(0x300026F0 + (i * 0x204));
                    if (playerObjectIndex != ushort.MaxValue)
                    {
                        int playerTableIndexOffset = playerObjectIndex * 12;
                        int playerTableObjectPointerAddress = 0x3003CAE8 + playerTableIndexOffset + 8;
                        int playerTableObjectAddress = _server.ServerMemory.ReadInt(playerTableObjectPointerAddress);
                        byte ObjectActive = _server.ServerMemory.ReadByte(playerTableObjectAddress + 7);
                        if (ObjectActive == 0x40)
                        {
                            var playerX = _server.ServerMemory.ReadFloat(playerTableObjectAddress + 0x64);
                            var playerY = _server.ServerMemory.ReadFloat(playerTableObjectAddress + 0x68);
                            var playerZ = _server.ServerMemory.ReadFloat(playerTableObjectAddress + 0x6C);
                            var playerCrouch = _server.ServerMemory.ReadByte(playerTableObjectAddress + 0x150);
                            if (playerX < 45F)
                            {

                                a++;
                                _server.ServerMemory.WriteMemory(false, playerTableObjectAddress + 0x208,
                                    new byte[] { 0x01, 0xFE, 0xFE, 0xFF });
                            }
                            if (playerX < 60f && playerY < 11f && playerZ > 13f)
                                _server.ServerMemory.WriteMemory(false, playerTableObjectAddress + 0x208,
                                    new byte[] { 0x01, 0xFE, 0xFE, 0xFF });

                            //114.5949,-8.560087,2.869605
                            //114.8247,-9.818688,2.869854
                            //playerX < 114.8 && playerX > 114.5 && playerY < -9.8 && playerY > -8.9 && playerY < -9.8 &&
                            //if (playerX > 114.5949f && playerY < -7.81f && playerCrouch == 1)
                            if (playerX < Xmax && playerX > Xmin && playerY < Ymax && playerY > Ymin && playerZ < Zmax && playerZ > Zmin)
                            {
                                await Task.Run(() =>
                                {
                                    _server.ServerMemory.WriteByte(playerTableObjectAddress + 0xC0, 0);
                                    for (int j = 0; j < 1024; j++)
                                    {
                                        //78.69836,3.918525,20.36737
                                        _server.ServerMemory.WriteFloat(playerTableObjectAddress + 100, 78.69f);
                                        _server.ServerMemory.WriteFloat(playerTableObjectAddress + 104, 3.91f);
                                        _server.ServerMemory.WriteFloat(playerTableObjectAddress + 108, 20.36f);
                                    }

                                    _server.ServerMemory.WriteByte(playerTableObjectAddress + 0xC0, 64);
                                });
                            }
                        }
                    }
                }

                if (a >= 5)
                    InGameFlop = false;
            }
        }

        public int tolerance { get; set; }
    }
}
