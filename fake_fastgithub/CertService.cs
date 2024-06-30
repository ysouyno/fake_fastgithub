using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography.X509Certificates;

namespace fake_fastgithub
{
    sealed class CertService
    {
        private const string CAPATH = "cacert";
        private const int KEY_SIZE_BITS = 2048;
        private readonly IMemoryCache serverCertCache;
        private readonly ILogger<CertService> logger;

        public string CaCerFilePath { get; } = $"{CAPATH}/{nameof(fake_fastgithub)}.cer";

        public string CaKeyFilePath { get; } = $"{CAPATH}/{nameof(fake_fastgithub)}.key";

        public CertService(IMemoryCache serverCertCache, ILogger<CertService> logger)
        {
            this.serverCertCache = serverCertCache;
            this.logger = logger;

            Directory.CreateDirectory(CAPATH);
        }

        public bool CreateCaCertIfNotExists()
        {
            if (File.Exists(this.CaCerFilePath) && File.Exists(this.CaKeyFilePath))
            {
                return false;
            }

            File.Delete(this.CaCerFilePath);
            File.Delete(this.CaKeyFilePath);

            var validFrom = DateTime.Today.AddDays(-1);
            var validTo = DateTime.Today.AddYears(10);
            CertGenerator.GenerateBySelf(new[] { nameof(fake_fastgithub) }, KEY_SIZE_BITS, validFrom, validTo, this.CaCerFilePath, this.CaKeyFilePath);
            return true;
        }

        public void InstallAndTrustCaCert()
        {
            if (OperatingSystem.IsWindows())
            {
                this.InstallAndTrustCaCertAtWindows();
            }
            else
            {
                this.logger.LogWarning($"不支持自动安装证书 {this.CaCerFilePath}，请根据你的系统平台手工安装和信任 CA 证书");
            }
        }

        private void InstallAndTrustCaCertAtWindows()
        {
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);

                var caCert = new X509Certificate2(this.CaCerFilePath);
                var subjectName = caCert.Subject[3..];
                foreach (var item in store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false))
                {
                    if (item.Thumbprint != caCert.Thumbprint)
                    {
                        store.Remove(item);
                    }
                }
                if (store.Certificates.Find(X509FindType.FindByThumbprint, caCert.Thumbprint, true).Count == 0)
                {
                    store.Add(caCert);
                }
                store.Close();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"安装证书 {this.CaCerFilePath} 失败，请手动安装到" +
                    $"“将所有的证书都放入下载存储”/“受信任的根证书颁发机构”", ex);
            }
        }

        public X509Certificate2 GetOrCreateServerCert(string? domain)
        {
            var key = $"{nameof(CertService)}:{domain}";
            return this.serverCertCache.GetOrCreate(key, GetOrCreateCert);

            // 生成域名的 1 年证书
            X509Certificate2 GetOrCreateCert(ICacheEntry entry)
            {
                var domains = GetDomains(domain).Distinct();
                var validFrom = DateTime.Today.AddDays(-1);
                var validTo = DateTime.Today.AddYears(1);

                entry.SetAbsoluteExpiration(validTo);
                return CertGenerator.GenerateByCa(domains, KEY_SIZE_BITS, validFrom, validTo, this.CaCerFilePath, this.CaKeyFilePath);
            }
        }

        private static IEnumerable<string> GetDomains(string? domain)
        {
            if (string.IsNullOrEmpty(domain) == false)
            {
                yield return domain;
                yield break;
            }

            yield return LocalMachine.Name;
            foreach (var address in LocalMachine.GetAllIPv4Addresses())
            {
                yield return address.ToString();
            }
        }
    }
}
