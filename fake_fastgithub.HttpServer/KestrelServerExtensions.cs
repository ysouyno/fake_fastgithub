using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using fake_fastgithub.Configuration;
using fake_fastgithub.HttpServer.Certs;
using fake_fastgithub.HttpServer.TlsMiddlewares;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Net.Security;

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

        private static ListenOptions UseTls(this ListenOptions listen, Func<string, X509Certificate2>certFactory)
        {
            var invadeMiddleware = listen.ApplicationServices.GetRequiredService<TlsInvadeMiddleware>();
            var restoreMiddleware = listen.ApplicationServices.GetRequiredService<TlsRestoreMiddleware>();

            listen.Use(next => context => invadeMiddleware.InvokeAsync(next, context));
            listen.UseHttps(new TlsHandshakeCallbackOptions
            {
                OnConnection = context =>
                {
                    var options = new SslServerAuthenticationOptions
                    {
                        ServerCertificate = certFactory(context.ClientHelloInfo.ServerName)
                    };
                    return ValueTask.FromResult(options);
                },
            });
            listen.Use(next => context => restoreMiddleware.InvokeAsync(next, context));

            return listen;
        }

        public static ListenOptions UseTls(this ListenOptions listen)
        {
            var certService = listen.ApplicationServices.GetRequiredService<CertService>();
            certService.CreateCaCertIfNotExists();
            certService.InstallAndTrustCaCert();
            return listen.UseTls(domain => certService.GetOrCreateServerCert(domain));
        }

        public static void ListenHttpsReverseProxy(this KestrelServerOptions kestrel)
        {
            var https_port = GlobalListener.HttpsPort;
            kestrel.ListenLocalhost(https_port, listen =>
            {
                if (OperatingSystem.IsWindows())
                {
                    // TODO
                }

                listen.UseTls();
            });

            if (OperatingSystem.IsWindows())
            {
                var logger = kestrel.GetLogger();
                logger.LogInformation($"已监听 https://localhost:{https_port}，https 反向代理服务启动完成");
            }
        }
    }
}
