using System.Net;
using System.Net.NetworkInformation;

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
            return tcpListeners.Any(item => item.Port == port);
        }
    }
}
