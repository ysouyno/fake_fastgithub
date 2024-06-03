using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace fake_fastgithub.HttpServer.Certs.CaCertInstallers
{
    sealed class CaCertInstallerOfWindows : ICaCertInstaller
    {
        private readonly ILogger<CaCertInstallerOfWindows> logger;

        public CaCertInstallerOfWindows(ILogger<CaCertInstallerOfWindows> logger)
        {
            this.logger = logger;
        }

        public bool IsSupported()
        {
            return OperatingSystem.IsWindows();
        }

        public void Install(string caCertFilePath)
        {
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);

                var caCert = new X509Certificate2(caCertFilePath);
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
            catch (Exception)
            {
                logger.LogWarning($"请手动安装 CA 证书 {caCertFilePath} 到“将所有的证书都放入下列存储”\\“受信任的根证书颁发机构”");
            }
        }
    }
}
