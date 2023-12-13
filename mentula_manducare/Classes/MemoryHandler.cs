using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MentulaManducare;

namespace mentula_manducare.Classes
{
    public class MemoryHandler
    {
        #region Flags
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }
        #endregion
        #region Externs


        [DllImport("user32.dll")]
        public static extern short GetKeyState(Keys nVirtKey);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        //public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, Uint nSize, out int lpNumberOfBytesWritten);
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, int lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        public static extern int OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(int hObject);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtectEx(int hProcess, int lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        // Find window by Caption only. Note you must pass 0 as the first parameter.
        public static extern int FindWindowByCaption(int ZeroOnly, string lpWindowName);
        //public static extern short GetKeyState(VirtualKeyStates nVirtKey);
        #endregion
        #region Const

        const uint PAGE_NOACCESS = 1;
        const uint PAGE_READONLY = 2;
        const uint PAGE_READWRITE = 4;
        const uint PAGE_WRITECOPY = 8;
        const uint PAGE_EXECUTE = 16;
        const uint PAGE_EXECUTE_READ = 32;
        const uint PAGE_EXECUTE_READWRITE = 64;
        const uint PAGE_EXECUTE_WRITECOPY = 128;
        const uint PAGE_GUARD = 256;
        const uint PAGE_NOCACHE = 512;
        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        #endregion

        private Process mainProcess { get; set; }
        private int processID { get; set; }
        private int processHandle { get; set; }
        private int processBase { get; set; }

        public MemoryHandler(Process process)
        {
            mainProcess = process;
            processID = process.Id;
            processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, mainProcess.Id);
            processBase = (int)process.MainModule.BaseAddress;
        }

        public bool ProcessIsRunning()
        {
            mainProcess.Refresh();
            return mainProcess.HasExited;
        }
        public int DllImageAddress(int handle, string dllname)
        {
            foreach (ProcessModule module in mainProcess.Modules)
                if (module.FileName.EndsWith(dllname))
                    return (int)module.BaseAddress;
            return -1;
        }

        public string NullTerminateString(string mystring)
        {
            try
            {
                char[] mychar = mystring.ToCharArray();
                string returnstring = "";
                for (int i = 0; i < mystring.Length; i++)
                    if (mychar[i] == '\0') return returnstring;
                    else if (mychar.Length == i) return returnstring;
                    else returnstring += mychar[i].ToString();
                return returnstring;
            }
            catch
            {
                return mystring.TrimEnd('0');
            }
        }

        public int ImageAddress(int pOffset)
        {
            return processBase + pOffset;
        }


        public byte[] ReadMemory(bool addToBaseAddress, int pOffset, int pSize)
        {
            byte[] buffer = new byte[pSize];
            ReadProcessMemory(processHandle, addToBaseAddress ? ImageAddress(pOffset) : pOffset, buffer, pSize, 0);
            return buffer;
        }

        public void WriteMemory(bool addToBaseAddress, int pOffset, byte[] pBytes)
        {
            WriteProcessMemory(processHandle, addToBaseAddress ? ImageAddress(pOffset) : pOffset, pBytes, pBytes.Length,
                0);
        }

        public int Pointer(bool addToBase, params int[] pOffsets)
        {
            var startPointer = ReadInt(pOffsets[0], true);
            for (var i = 1; i < pOffsets.Length; i++)
                if (i == pOffsets.Length - 1)
                    startPointer += pOffsets[i];
                else
                    startPointer = ReadInt(startPointer + pOffsets[i]);
            return startPointer;
        }

        public int BlamCachePointer(int cacheOffset)
        {
            return Pointer(true, 0x4A29BC, cacheOffset);
        }
        #region Read Memory

        public byte ReadByte(int pOffset, bool addToBase = false) => 
            ReadMemory(addToBase, pOffset, 1)[0];

        public bool ReadBool(int pOffset, bool addToBase = false) =>
            ReadByte(pOffset, addToBase) == 1;

        public short ReadShort(int pOffset, bool addToBase = false) => 
            BitConverter.ToInt16(ReadMemory(addToBase, pOffset, 2), 0);

        public ushort ReadUShort(int pOffset, bool addToBase = false) =>
            BitConverter.ToUInt16(ReadMemory(addToBase, pOffset, 2), 0);

        public int ReadInt(int pOffset, bool addToBase = false) => 
            BitConverter.ToInt32(ReadMemory(addToBase, pOffset, 4), 0);

        public uint ReadUInt(int pOffset, bool addToBase = false) =>
            BitConverter.ToUInt32(ReadMemory(addToBase, pOffset, 4), 0);

        public long ReadLong(int pOffset, bool addToBase = false) => 
            BitConverter.ToInt64(ReadMemory(addToBase, pOffset, 8), 0);

        public ulong ReadULong(int pOffset, bool addToBase = false) =>
            BitConverter.ToUInt64(ReadMemory(addToBase, pOffset, 8), 0);

        public float ReadFloat(int pOffset, bool addToBase = false) =>
            BitConverter.ToSingle(ReadMemory(addToBase, pOffset, 4), 0);

        public double ReadDouble(int pOffset, bool addToBase = false) =>
            BitConverter.ToDouble(ReadMemory(addToBase, pOffset, 8), 0);

        public string ReadStringAscii(int pOffset, int length, bool addToBase = false) =>
            NullTerminateString(Encoding.ASCII.GetString(ReadMemory(addToBase, pOffset, length)));

        public string ReadStringUnicode(int pOffset, int length, bool addToBase = false) =>
            NullTerminateString(Encoding.Unicode.GetString(ReadMemory(addToBase, pOffset, length * 2)));

        #endregion
        #region Write Memory

        public void WriteByte(int pOffset, byte pByte, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, new byte[] {pByte});

        public void WriteBool(int pOffset, bool pByte, bool addToBase = false) =>
            WriteByte(pOffset, (pByte) ?  (byte)1 :(byte)0, addToBase);

        public void WriteShort(int pOffset, short pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, BitConverter.GetBytes(pBytes));

        public void WriteUShort(int pOffset, ushort pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, BitConverter.GetBytes(pBytes));

        public void WriteInt(int pOffset, int pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, BitConverter.GetBytes(pBytes));

        public void WriteUInt(int pOffset, uint pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, BitConverter.GetBytes(pBytes));

        public void WriteLong(int pOffset, long pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, BitConverter.GetBytes(pBytes));

        public void WriteULong(int pOffset, ulong pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, BitConverter.GetBytes(pBytes));

        public void WriteFloat(int pOffset, float pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, BitConverter.GetBytes(pBytes));

        public void WriteDouble(int pOffset, double pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, BitConverter.GetBytes(pBytes));

        public void WriteStringAscii(int pOffest, string pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffest, Encoding.ASCII.GetBytes(pBytes));

        public void WriteStringUnicode(int pOffset, string pBytes, bool addToBase = false) =>
            WriteMemory(addToBase, pOffset, Encoding.Unicode.GetBytes(pBytes));

        #endregion
    }
}
