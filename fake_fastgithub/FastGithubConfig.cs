using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;

namespace fake_fastgithub
{
    public class FastGithubConfig
    {
        private readonly ILogger<FastGithubConfig> logger;
        private SortedDictionary<DomainPattern, DomainConfig> domainConfigs;
        private ConcurrentDictionary<string, DomainConfig?> domainConfigCache;
        public IPEndPoint PureDns { get; private set; }
        public IPEndPoint FastDns { get; private set; }

        private static SortedDictionary<DomainPattern, DomainConfig> ConvertDomainConfigs(
            Dictionary<string, DomainConfig> domainConfigs)
        {
            var result = new SortedDictionary<DomainPattern, DomainConfig>();
            foreach (var kv in domainConfigs)
            {
                result.Add(new DomainPattern(kv.Key), kv.Value);
            }
            return result;
        }

        private void Update(FastGithubOptions options)
        {
            try
            {
                PureDns = options.PureDns.ToIPEndPoint();
                FastDns = options.FastDns.ToIPEndPoint();
                domainConfigs = ConvertDomainConfigs(options.DomainConfigs);
                domainConfigCache = new ConcurrentDictionary<string, DomainConfig?>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }

        public FastGithubConfig(
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<FastGithubConfig> logger)
        {
            this.logger = logger;
            var opt = options.CurrentValue;

            PureDns = opt.PureDns.ToIPEndPoint();
            FastDns = opt.FastDns.ToIPEndPoint();
            domainConfigs = ConvertDomainConfigs(opt.DomainConfigs);
            domainConfigCache = new ConcurrentDictionary<string, DomainConfig?>();

            options.OnChange(opt => Update(opt));
        }

        public bool IsMatch(string domain)
        {
            return domainConfigs.Keys.Any(item => item.IsMatch(domain));
        }
    }
}
