namespace fake_fastgithub
{
    sealed class DnscryptProxyHostedService : IHostedService
    {
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DnscryptProxyHostedService> logger;
        private DnscryptProxy? dnscryptProxy;

        public DnscryptProxyHostedService(FastGithubConfig fastGithubConfig, ILogger<DnscryptProxyHostedService> logger)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// 启动 dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var pureDns = fastGithubConfig.PureDns;
            if (LocalMachine.ContainsIPAddress(pureDns.Address) == true)
            {
                dnscryptProxy = new DnscryptProxy(pureDns);
                try
                {
                    await dnscryptProxy.StartAsync(cancellationToken);
                    logger.LogInformation($"{dnscryptProxy} 启动成功");
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"{dnscryptProxy} 启动失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 停止 dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (dnscryptProxy != null)
            {
                try
                {
                    dnscryptProxy.Stop();
                    logger.LogInformation($"{dnscryptProxy} 已停止");
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"{dnscryptProxy} 停止失败：{ex.Message}");
                }
            }

            return Task.CompletedTask;
        }
    }
}
