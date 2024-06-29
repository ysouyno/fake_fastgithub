using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace fake_fastgithub
{
    public static class LocalMachine
    {
        public static string Name => Environment.MachineName;

        public static IEnumerable<IPAddress> GetAllIPAddresses()
        {
            yield return IPAddress.Loopback;
            yield return IPAddress.IPv6Loopback;

            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addressInfo in @interface.GetIPProperties().UnicastAddresses)
                {
                    yield return addressInfo.Address;
                }
            }
        }

        public static IEnumerable<IPAddress> GetAllIPv4Addresses()
        {
            foreach (var address in GetAllIPAddresses())
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    yield return address;
                }
            }
        }

        public static bool CanListenTcp(int port)
        {
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return tcpListeners.Any(item => item.Port == port) == false;
        }

        public static bool ContainsIPAddress(IPAddress address)
        {
            return GetAllIPAddresses().Contains(address);
        }

        /// <summary>
        /// 获取与远程节点通讯的本机 IP 地址
        /// </summary>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <returns></returns>
        public static IPAddress? GetLocalIPAddress(EndPoint remoteEndPoint)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(remoteEndPoint);
                return socket.LocalEndPoint is IPEndPoint localEndPoint ? localEndPoint.Address : default;
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static bool CanListenUdp(int port)
        {
            var udpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
            return udpListeners.Any(item => item.Port == port) == false;
        }
    }
}
