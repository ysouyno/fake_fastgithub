using DNS.Client;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Net;
using DNS.Protocol;
using System.Net.Sockets;

namespace fake_fastgithub
{
    sealed class DomainResolver : IDomainResolver
    {
        private readonly IMemoryCache memoryCache;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DomainResolver> logger;

        private readonly TimeSpan lookupTimeout = TimeSpan.FromSeconds(2d);
        private readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(2d);
        private readonly TimeSpan resolveCacheTimeSpan = TimeSpan.FromMinutes(2d);
        private readonly ConcurrentDictionary<DnsEndPoint, SemaphoreSlim> semaphoreSlims = new();

        public DomainResolver(IMemoryCache memoryCache, FastGithubConfig fastGithubConfig, ILogger<DomainResolver> logger)
        {
            this.memoryCache = memoryCache;
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        private async Task<IPAddress?> IsAvailableAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            try
            {
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                using var timeoutTokenSource = new CancellationTokenSource(this.connectTimeout);
                using var linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                await socket.ConnectAsync(address, port, linkedTokenSource.Token);
                return address;
            }
            catch (OperationCanceledException)
            {
                return default;
            }
            catch (Exception)
            {
                await Task.Delay(connectTimeout, cancellationToken);
                return default;
            }
        }

        private async Task<IPAddress?> GetFastIPAddressAsync(IEnumerable<IPAddress> addresses, int port,
            CancellationToken cancellationToken)
        {
            var tasks = addresses.Select(address => IsAvailableAsync(address, port, cancellationToken));
            var fastTask = await Task.WhenAny(tasks);
            return await fastTask;
        }

        private async Task<IPAddress> LookupCoreAsync(IPEndPoint dns, DnsEndPoint endPoint,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"LookupCoreAsync: dns: {dns}, endPoint: {endPoint}");

            var dnsClient = new DnsClient(dns);
            using var timeoutTokenSource = new CancellationTokenSource(lookupTimeout);
            using var linkedTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

            var addresses = await dnsClient.Lookup(endPoint.Host, RecordType.A, linkedTokenSource.Token);
            var fastAddress = await GetFastIPAddressAsync(addresses, endPoint.Port, cancellationToken);
            if (fastAddress != null)
            {
                logger.LogInformation($"[{endPoint.Host}->{fastAddress}]");
                return fastAddress;
            }

            throw new Exception($"dns {dns} 解析不到 {endPoint.Host} 可用的 ip");
        }

        private async Task<IPAddress> LookupAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
        {
            var pureDns = fastGithubConfig.PureDns;
            var fastDns = fastGithubConfig.FastDns;

            try
            {
                return await LookupCoreAsync(pureDns, endPoint, cancellationToken);
            }
            catch (Exception)
            {
                this.logger.LogWarning($"由于 {pureDns} 解析 {endPoint.Host} 失败，本次使用 {fastDns}");
                return await LookupCoreAsync(fastDns, endPoint, cancellationToken); ;
            }
        }

        public async Task<IPAddress> ResolveAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default)
        {
            var semaphore = semaphoreSlims.GetOrAdd(endPoint, _ => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync(cancellationToken);
                if (memoryCache.TryGetValue<IPAddress>(endPoint, out var address) == false)
                {
                    address = await LookupAsync(endPoint, cancellationToken);
                    memoryCache.Set(endPoint, address, resolveCacheTimeSpan);
                }
                return address;
            }
            finally { semaphore.Release(); }
        }
    }
}
