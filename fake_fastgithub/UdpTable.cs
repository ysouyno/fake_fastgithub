using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace fake_fastgithub
{
    unsafe static class UdpTable
    {
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        private enum UDP_TABLE_CLASS
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(
            void* pUdpTable,
            ref int pdwSize,
            bool bOrder,
            AddressFamily ulAf,
            UDP_TABLE_CLASS tableClass,
            uint reserved = 0);

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint localAddr;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;

            public int owningPid;

            public int ProcessId => owningPid;

            public IPAddress LocalAddress => new(localAddr);

            public ushort LocalPort => BinaryPrimitives.ReadUInt16BigEndian(this.localPort);
        }

        public static bool TryGetOwnerProcessId(int port, out int processId)
        {
            processId = 0;
            var pdwSize = 0;
            var result = GetExtendedUdpTable(null, ref pdwSize, false, AddressFamily.InterNetwork,
                UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
            if (result != ERROR_INSUFFICIENT_BUFFER)
            {
                return false;
            }

            var buffer = new byte[pdwSize];
            fixed (byte* pUdpTable = &buffer[0])
            {
                result = GetExtendedUdpTable(pUdpTable, ref pdwSize, false, AddressFamily.InterNetwork,
                    UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
                if (result != 0)
                {
                    return false;
                }

                var prt = new IntPtr(pUdpTable);
                var table = Marshal.PtrToStructure<MIB_UDPTABLE_OWNER_PID>(prt);
                prt += sizeof(int);
                for (var i = 0; i < table.dwNumEntries; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(prt);
                    if (row.LocalPort == port)
                    {
                        processId = row.ProcessId;
                        return true;
                    }

                    prt += Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();
                }
            }

            return false;
        }

        public static bool KillPortOwner(int port)
        {
            if (TryGetOwnerProcessId(port, out var pid) == false)
            {
                return true;
            }

            try
            {
                var proess = Process.GetProcessById(pid);
                proess.Kill();
                return proess.WaitForExit(1000);
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
