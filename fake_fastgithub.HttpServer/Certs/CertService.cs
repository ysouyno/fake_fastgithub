using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace fake_fastgithub.HttpServer.Certs
{
    sealed class CertService
    {
        private const string CACERT_PATH = "cacert";
        private readonly IMemoryCache serverCertCache;
        private readonly IEnumerable<ICaCertInstaller> certInstallers;
        private readonly ILogger<CertService> logger;
        private X509Certificate2? caCert;

        /// <summary>
        /// *.crt 或者 *.cer 文件路径
        /// </summary>
        public string CaCerFilePath { get; } =
            OperatingSystem.IsLinux() ? $"{CACERT_PATH}/fake_fastgithub.crt" : $"{CACERT_PATH}/fake_fastgithub.cer";

        public string CaKeyFilePath { get; } = $"{CACERT_PATH}/fake_fastgithub.key";

        public CertService(
            IMemoryCache serverCertCache,
            IEnumerable<ICaCertInstaller> certInstallers,
            ILogger<CertService> logger)
        {
            this.serverCertCache = serverCertCache;
            this.certInstallers = certInstallers;
            this.logger = logger;
            Directory.CreateDirectory(CACERT_PATH);
        }

        public bool CreateCaCertIfNotExists()
        {
            if (File.Exists(CaCerFilePath) && File.Exists(CaKeyFilePath))
            {
                return false;
            }

            File.Delete(CaCerFilePath);
            File.Delete(CaKeyFilePath);

            var notBefore = DateTimeOffset.Now.AddDays(-1);
            var notAfter = DateTimeOffset.Now.AddYears(10);

            var subjectName = new X500DistinguishedName($"CN={nameof(fake_fastgithub)}");
            caCert = CertGenerator.CreateCACertificate(subjectName, notBefore, notAfter);

            var privateKeyPem = caCert.GetRSAPrivateKey()?.ExportRSAPrivateKeyPem();
            File.WriteAllText(this.CaKeyFilePath, new string(privateKeyPem), Encoding.ASCII);

            var certPem = this.caCert.ExportCertificatePem();
            File.WriteAllText(this.CaCerFilePath, new string(certPem), Encoding.ASCII);

            return true;
        }

        public static bool GitConfigSslverify(bool value)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"config --global http.sslverify {value.ToString().ToLower()}",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void InstallAndTrustCaCert()
        {
            var installer = certInstallers.FirstOrDefault(item => item.IsSupported());
            if (installer != null)
            {
                installer.Install(CaCerFilePath);
            }
            else
            {
                logger.LogWarning($"请根据你的系统平台手动安装和信任 CA 证书{this.CaCerFilePath}");
            }

            GitConfigSslverify(false);
        }

        private static IEnumerable<string> GetExtraDomains()
        {
            yield return Environment.MachineName;
            yield return IPAddress.Loopback.ToString();
            yield return IPAddress.IPv6Loopback.ToString();
        }

        public X509Certificate2 GetOrCreateServerCert(string? domain)
        {
            if (this.caCert == null)
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(File.ReadAllText(this.CaKeyFilePath));
                this.caCert = new X509Certificate2(this.CaCerFilePath).CopyWithPrivateKey(rsa);
            }

            var key = $"{nameof(CertService)}:{domain}";
            var endCert = this.serverCertCache.GetOrCreate(key, GetOrCreateCert);
            return endCert!;

            X509Certificate2 GetOrCreateCert(ICacheEntry entry)
            {
                var notBefore = DateTimeOffset.Now.AddDays(-1);
                var notAfter = DateTimeOffset.Now.AddYears(1);
                entry.SetAbsoluteExpiration(notAfter);

                var extraDomains = GetExtraDomains();

                var subjectName = new X500DistinguishedName($"CN={domain}");
                var endCert = CertGenerator.CreateEndCertificate(this.caCert, subjectName, extraDomains, notBefore, notAfter);

                return new X509Certificate2(endCert.Export(X509ContentType.Pfx));
            }
        }
    }
}
