using System.Net.NetworkInformation;

namespace fake_fastgithub
{
    public static class LocalMachine
    {
        public static bool CanListenTcp(int port)
        {
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return tcpListeners.Any(item => item.Port == port);
        }
    }
}
