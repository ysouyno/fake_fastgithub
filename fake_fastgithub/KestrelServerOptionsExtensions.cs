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
    }
}
