namespace fake_fastgithub
{
    public class FastGithubOptions
    {
        public DnsConfig PureDns { get; set; } = new DnsConfig { IPAddress = "127.0.0.1", Port = 5533 };
        public DnsConfig FastDns { get; set; } = new DnsConfig { IPAddress = "114.114.114.114", Port = 53 };
        public Dictionary<string, DomainConfig> DomainConfigs { get; set; } = new();
    }
}
