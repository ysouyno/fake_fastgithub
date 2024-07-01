using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace fake_fastgithub
{
    public static class KestrelServerOptionsExtensions
    {
        private static ILogger GetLogger(this KestrelServerOptions kestrel)
        {
            var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger($"{nameof(fake_fastgithub)}");
        }

        public static void ListenGithubSshProxy(this KestrelServerOptions kestrel)
        {
            const int SSH_PORT = 22;
            var logger = kestrel.GetLogger();

            if (LocalMachine.CanListenTcp(SSH_PORT) == false)
            {
                logger.LogWarning($"由于 tcp 端口 {SSH_PORT} 已经被其它进程占用，github 的 ssh 代理功能将受限");
            }
            else
            {
                kestrel.Listen(IPAddress.Any, SSH_PORT,
                    listen => listen.UseConnectionHandler<GithubSshHandler>());
                logger.LogInformation("已监听 github 的 ssh 代理");
            }
        }

        public static void ListenHttpReverseProxy(this KestrelServerOptions kestrel)
        {
            const int HTTP_PORT = 80;
            var logger = kestrel.GetLogger();

            if (LocalMachine.CanListenTcp(HTTP_PORT) == false)
            {
                logger.LogWarning($"由于 tcp 端口 {HTTP_PORT} 已经被其它进程占用，http 反向代理功能将受限");
            }
            else
            {
                kestrel.Listen(IPAddress.Any, HTTP_PORT);
                logger.LogInformation($"已监听 http  反向代理");
            }
        }

        public static void ListenHttpsReverseProxy(this KestrelServerOptions kestrel)
        {
            const int HTTPS_PORT = 443;

            if (OperatingSystem.IsWindows())
            {
                TcpTable.KillPortOwner(HTTPS_PORT);
            }

            if (LocalMachine.CanListenTcp(HTTPS_PORT) == false)
            {
                throw new Exception($"由于 tcp 端口 {HTTPS_PORT} 已经被其它进程占用，" +
                    $"{nameof(fake_fastgithub)} 无法进行必须的 https 反向代理");
            }

            var certService = kestrel.ApplicationServices.GetRequiredService<CertService>();
            certService.CreateCaCertIfNotExists();
            certService.InstallAndTrustCaCert();

            kestrel.Listen(IPAddress.Any, HTTPS_PORT,
                listen => listen.UseHttps(https =>
                https.ServerCertificateSelector = (ctx, domain) =>
                certService.GetOrCreateServerCert(domain)));

            var logger = kestrel.GetLogger();
            logger.LogInformation($"已监听 https 反向代理");
        }
    }
}
