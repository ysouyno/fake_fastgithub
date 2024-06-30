using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace fake_fastgithub
{
    static class SystemDnsUtil
    {
        private static readonly IPAddress www_baidu_com = IPAddress.Parse("183.232.231.172");

        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        public static extern void DnsFlushResolverCache();

        [DllImport("iphlpapi")]
        private static extern int GetBestInterface(uint dwDestAddr, ref uint pdwBestIfIndex);

        private static NetworkInterface GetBestNetworkInterface(IPAddress remoteAddress)
        {
            var dwBestIfIndex = 0u;
            var dwDestAddr = BitConverter.ToUInt32(remoteAddress.GetAddressBytes());
            var errorCode = GetBestInterface(dwDestAddr, ref dwBestIfIndex);
            if (errorCode != 0)
            {
                throw new NetworkInformationException(errorCode);
            }

            var @interface = NetworkInterface.GetAllNetworkInterfaces()
                .Where(item => item.GetIPProperties().GetIPv4Properties().Index == dwBestIfIndex)
                .FirstOrDefault();

            return @interface ?? throw new Exception("找不到网络适配器用来设置 dns");
        }

        private static void SetNameServers(NetworkInterface @interface, IEnumerable<IPAddress> nameServers)
        {
            Netsh($@"interface ipv4 delete dns ""{@interface.Name}"" all");
            foreach (var address in nameServers)
            {
                Netsh($@"interface ipv4 add dns ""{@interface.Name}"" {address} validate=no");
            }

            static void Netsh(string arguments)
            {
                var netsh = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                Process.Start(netsh)?.WaitForExit();
            }
        }

        public static void DnsSetPrimitive(IPAddress primitive)
        {
            var @interface = GetBestNetworkInterface(www_baidu_com);
            var dnsAddresses = @interface.GetIPProperties().DnsAddresses;
            if (primitive.Equals(dnsAddresses.FirstOrDefault()) == false)
            {
                var nameServers = dnsAddresses.Prepend(primitive);
                SetNameServers(@interface, nameServers);
            }
        }

        public static void DnsRemovePrimitive(IPAddress primitive)
        {
            var @interface = GetBestNetworkInterface(www_baidu_com);
            var dnsAddresses = @interface.GetIPProperties().DnsAddresses;
            if (primitive.Equals(dnsAddresses.FirstOrDefault()))
            {
                var nameServers = dnsAddresses.Skip(1);
                SetNameServers(@interface, nameServers);
            }
        }
    }
}
