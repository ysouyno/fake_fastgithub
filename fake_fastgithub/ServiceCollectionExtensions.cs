using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace fake_fastgithub
{
    public static class ServiceCollectionExtensions
    {
        public static OptionsBuilder<FastGithubOptions> AddConfiguration(this IServiceCollection services)
        {
            services.TryAddSingleton<FastGithubOptions>();
            return services.AddOptions<FastGithubOptions>();
        }

        public static IServiceCollection AddDnsServer(this IServiceCollection services)
        {
            services.TryAddSingleton<RequestResolver>();
            services.TryAddSingleton<DnsServer>();
            services.AddSingleton<IDnsValidator, HostsValidator>();
            services.AddSingleton<IDnsValidator, ProxyValidtor>();
            return services.AddHostedService<DnsHostedService>();
        }

        public static IServiceCollection AddDomainResolve(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.TryAddSingleton<IDomainResolver, DomainResolver>();
            return services.AddHostedService<DnscryptProxyHostedService>();
        }
    }
}
