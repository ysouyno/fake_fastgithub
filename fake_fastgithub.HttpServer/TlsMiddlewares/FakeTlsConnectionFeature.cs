using Microsoft.AspNetCore.Http.Features;
using System.Security.Cryptography.X509Certificates;

namespace fake_fastgithub.HttpServer.TlsMiddlewares
{
    sealed class FakeTlsConnectionFeature : ITlsConnectionFeature
    {
        public static FakeTlsConnectionFeature Instance { get; } = new FakeTlsConnectionFeature();

        public X509Certificate2? ClientCertificate
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
