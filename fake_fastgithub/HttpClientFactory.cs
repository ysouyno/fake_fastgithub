using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace fake_fastgithub
{
    sealed class HttpClientFactory : IHttpClientFactory
    {
        private readonly IDomainResolver domainResolver;
        private ConcurrentDictionary<DomainConfig, HttpClientHandler> domainHandlers = new();

        public HttpClientFactory(IDomainResolver domainResolver,
            IOptionsMonitor<FastGithubOptions> options)
        {
            this.domainResolver = domainResolver;
            options.OnChange(opt => domainHandlers = new());
        }

        public HttpClient CreateHttpClient(DomainConfig domainConfig)
        {
            var httpClientHandler = domainHandlers.GetOrAdd(domainConfig,
                config => new HttpClientHandler(config, domainResolver));
            return new HttpClient(httpClientHandler, disposeHandler: false);
        }
    }
}
