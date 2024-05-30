using System.Net;
using System.Net.NetworkInformation;

namespace fake_fastgithub.Configuration
{
    public static class GlobalListener
    {
        private static readonly IPGlobalProperties global = IPGlobalProperties.GetIPGlobalProperties();
        private static readonly HashSet<int> tcp_listen_ports = GetListenPorts(global.GetActiveTcpListeners);

        private static HashSet<int> GetListenPorts(Func<IPEndPoint[]> func)
        {
            var hash_set = new HashSet<int>();
            try
            {
                foreach (var endpoint in func())
                {
                    hash_set.Add(endpoint.Port);
                }
            }
            catch (Exception) { }

            return hash_set;
        }

        private static int GetAvailablePort(Func<int, bool> can_func, int min_port)
        {
            for (var port = min_port; port < IPEndPoint.MaxPort; port++)
            {
                if (can_func(port) == true)
                {
                    return port;
                }
            }
            throw new FakeFastGithubException("当前无可用的端口");
        }

        public static bool CanListenTcp(int port)
        {
            return tcp_listen_ports.Contains(port) == false;
        }

        public static int GetAvailableTcpPort(int min_port)
        {
            return GetAvailablePort(CanListenTcp, min_port);
        }

        public static int HttpsPort { get; } = OperatingSystem.IsWindows() ? GetAvailableTcpPort(443) : GetAvailableTcpPort(38443);
    }
}
