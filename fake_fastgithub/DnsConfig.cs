using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace fake_fastgithub
{
    public record DnsConfig
    {
        [AllowNull]
        public string IPAddress { get; init; }

        public int Port { get; init; } = 53;

        public IPEndPoint ToIPEndPoint()
        {
            if (System.Net.IPAddress.TryParse(this.IPAddress, out var address) == false)
            {
                throw new Exception($"无效的 ip: {this.IPAddress}");
            }

            if (this.Port == 53 && LocalMachine.ContainsIPAddress(address))
            {
                throw new Exception($"配置的 dns 值不能指向 {nameof(fake_fastgithub)} 自身: {this.IPAddress}:{this.Port}");
            }

            return new IPEndPoint(address, this.Port);
        }
    }
}
