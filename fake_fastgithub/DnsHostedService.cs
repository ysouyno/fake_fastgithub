using Microsoft.Extensions.Options;
using System.Net;

namespace fake_fastgithub
{
    sealed class DnsHostedService : BackgroundService
    {
        private readonly DnsServer dnsServer;
        private readonly IEnumerable<IDnsValidator> dnsValidators;
        private readonly ILogger<DnsHostedService> logger;

        public DnsHostedService(IOptionsMonitor<FastGithubConfig> options,
            DnsServer dnsServer,
            IEnumerable<IDnsValidator> dnsValidators,
            ILogger<DnsHostedService> logger)
        {
            this.dnsServer = dnsServer;
            this.dnsValidators = dnsValidators;
            this.logger = logger;

            options.OnChange(opt =>
            {
                if (OperatingSystem.IsWindows())
                {
                    SystemDnsUtil.DnsFlushResolverCache();
                }
            });
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            dnsServer.Bind(IPAddress.Any, 53);
            logger.LogInformation("DNS 服务启动成功");

            try
            {
                SystemDnsUtil.DnsSetPrimitive(IPAddress.Loopback);
                SystemDnsUtil.DnsFlushResolverCache();
                logger.LogInformation("设置成本机主 DNS 成功");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"设置成本机主 DNS 为 {IPAddress.Loopback} 失败：{ex.Message}");
            }

            foreach (var item in dnsValidators)
            {
                await item.ValidateAsync();
            }

            await base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return dnsServer.ListenAsync(stoppingToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            dnsServer.Dispose();
            logger.LogInformation("DNS 服务已停止");

            try
            {
                SystemDnsUtil.DnsFlushResolverCache();
                SystemDnsUtil.DnsRemovePrimitive(IPAddress.Loopback);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"恢复 DNS 记录失败：{ex.Message}");
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
