using Microsoft.Extensions.Options;

namespace fake_fastgithub
{
    sealed class ProxyValidtor : IDnsValidator
    {
        private readonly IOptions<FastGithubOptions> options;
        private readonly ILogger<ProxyValidtor> logger;

        public ProxyValidtor(IOptions<FastGithubOptions> options, ILogger<ProxyValidtor> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        private void ValidateSystemProxy()
        {
            var systemProxy = HttpClient.DefaultProxy;
            if (systemProxy == null)
            {
                return;
            }

            foreach (var domain in options.Value.DomainConfigs.Keys)
            {
                var destination = new Uri($"https://{domain.Replace('*', 'a')}");
                var proxyServer = systemProxy.GetProxy(destination);
                if (proxyServer != null)
                {
                    logger.LogError($"由于系统配置了 {proxyServer} 代理 {domain} ，所以无法加速相关域名");
                }
            }
        }

        public Task ValidateAsync()
        {
            try
            {
                ValidateSystemProxy();
            }
            catch (Exception) { }

            return Task.CompletedTask;
        }
    }
}
