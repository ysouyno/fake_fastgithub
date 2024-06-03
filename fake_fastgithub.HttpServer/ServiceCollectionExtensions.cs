using fake_fastgithub.HttpServer.Certs;
using Microsoft.Extensions.DependencyInjection;
using fake_fastgithub.HttpServer.Certs.CaCertInstallers;
using fake_fastgithub.HttpServer.TlsMiddlewares;

namespace fake_fastgithub
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddReverseProxy(this IServiceCollection services)
        {
            // TODO
            return services
                .AddMemoryCache()
                .AddHttpForwarder()
                .AddSingleton<CertService>()
                .AddSingleton<ICaCertInstaller, CaCertInstallerOfWindows>()
                .AddSingleton<TlsInvadeMiddleware>()
                .AddSingleton<TlsRestoreMiddleware>();
        }
    }
}
