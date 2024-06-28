using System.Diagnostics;
using System.Net;

namespace fake_fastgithub
{
    sealed class DnscryptProxy
    {
        private const string name = "dnscrypt-proxy";
        private Process? process;
        public IPEndPoint EndPoint { get; }

        public DnscryptProxy(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        private static Process? StartDnscryptProxy(string arguments)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = $"{name}.exe",
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var tomlPath = $"{name}.toml";
            await TomlUtil.SetListenAsync(tomlPath, EndPoint, cancellationToken);
            await TomlUtil.SetEdnsClientSubnetAsync(tomlPath, cancellationToken);

            foreach (var process in Process.GetProcessesByName(name))
            {
                process.Kill();
                process.WaitForExit();
            }

            StartDnscryptProxy("-service uninstall")?.WaitForExit();
            StartDnscryptProxy("-service install")?.WaitForExit();
            StartDnscryptProxy("-service start")?.WaitForExit();

            process = Process.GetProcessesByName(name).FirstOrDefault();
        }

        public void Stop()
        {
            StartDnscryptProxy("-service stop")?.WaitForExit();
            StartDnscryptProxy("-service uninstall")?.WaitForExit();

            if (process != null && process.HasExited == false)
                process.Kill();
        }
    }
}
