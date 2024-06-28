using System.Net;

namespace fake_fastgithub
{
    public struct TlsSniPattern
    {
        public string Value { get; }
        public static TlsSniPattern None { get; } = new TlsSniPattern(string.Empty);
        public static TlsSniPattern Domain { get; } = new TlsSniPattern("@domain");
        public static TlsSniPattern IPAddress { get; } = new TlsSniPattern("@ipaddress");
        public static TlsSniPattern Random { get; } = new TlsSniPattern("@random");

        public TlsSniPattern(string? value)
        {
            Value = value ?? string.Empty;
        }

        public TlsSniPattern WithDomain(string domain)
        {
            var value = Value.Replace(Domain.Value, domain, StringComparison.OrdinalIgnoreCase);
            return new TlsSniPattern(value);
        }

        public TlsSniPattern WithIPAddress(IPAddress address)
        {
            var value = Value.Replace(IPAddress.Value, address.ToString(), StringComparison.OrdinalIgnoreCase);
            return new TlsSniPattern(value);
        }

        public TlsSniPattern WithRandom()
        {
            var value = Value.Replace(Random.Value, Environment.TickCount64.ToString(), StringComparison.OrdinalIgnoreCase);
            return new TlsSniPattern(value);
        }

        public override string ToString()
        {
            return this.Value;
        }
    }
}
