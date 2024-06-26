using System.Net;

namespace fake_fastgithub
{
    public interface IDomainResolver
    {
        Task<IPAddress> ResolveAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default);
    }
}
