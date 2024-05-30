using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fake_fastgithub
{
    public static class KestrelServerExtensions
    {
        private static ILogger GetLogger(this KestrelServerOptions kestrel)
        {
            var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger($"{nameof(fake_fastgithub)}.HttpServer");
        }

        public static void NoLimit(this KestrelServerOptions kestrel)
        {
            kestrel.Limits.MaxRequestBodySize = null;
            kestrel.Limits.MinResponseDataRate = null;
            kestrel.Limits.MinRequestBodyDataRate = null;
        }

        private static void ListenHttpsReverseProxy(this KestrelServerOptions kestrel)
        {
            var https_port = 443; // GlobalListener.HttpsPort;
            kestrel.ListenLocalhost(https_port, listen =>
            {
                // TODO
            });

            if (OperatingSystem.IsWindows())
            {
                var logger = kestrel.GetLogger();
                logger.LogInformation($"已监听 https://localhost:{https_port}，https 反向代理服务启动完成");
            }
        }
    }
}
