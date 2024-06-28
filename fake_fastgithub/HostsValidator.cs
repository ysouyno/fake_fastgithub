using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace fake_fastgithub
{
    sealed class HostsValidator : IDnsValidator
    {
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<HostsValidator> logger;

        public HostsValidator(FastGithubConfig fastGithubConfig, ILogger<HostsValidator> logger)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        public async Task ValidateAsync()
        {
            var hostsPath = @"/etc/hosts";
            if (OperatingSystem.IsWindows())
            {
                hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    $"drivers/{hostsPath}");
            }

            if (File.Exists(hostsPath) == false)
            {
                return;
            }

            var localAddresses = LocalMachine.GetAllIPv4Addresses().ToArray();
            var lines = await File.ReadAllLinesAsync(hostsPath);
            foreach (var line in lines)
            {
                if (HostsRecord.TryParse(line, out var record) == false)
                    continue;

                if (localAddresses.Contains(record.Address) == true)
                    continue;

                if (fastGithubConfig.IsMatch(record.Domain))
                {
                    logger.LogError($"由于 hosts 文件设置了 {record}，所以无法加速此域名");
                }
            }
        }

        private class HostsRecord
        {
            public string Domain { get; }

            public IPAddress Address { get; }

            private HostsRecord(string domain, IPAddress address)
            {
                Domain = domain;
                Address = address;
            }

            public override string ToString()
            {
                return $"[{Domain}->{Address}]";
            }

            public static bool TryParse(string record, [MaybeNullWhen(false)] out HostsRecord value)
            {
                value = null;
                if (record.TrimStart().StartsWith("#"))
                {
                    return false;
                }

                var items = record.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length < 2)
                {
                    return false;
                }

                if (IPAddress.TryParse(items[0], out var address) == false)
                {
                    return false;
                }

                value = new HostsRecord(items[1], address);
                return true;
            }
        }
    }
}
