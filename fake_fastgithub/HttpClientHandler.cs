using System.Collections;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;

namespace fake_fastgithub
{
    class HttpClientHandler : DelegatingHandler
    {
        private readonly DomainConfig domainConfig;
        private readonly IDomainResolver domainResolver;

        private static IEnumerable<string> ReadDnsNames(X509Certificate? cert)
        {
            if (cert == null)
                yield break;

            var parser = new Org.BouncyCastle.X509.X509CertificateParser();
            var x509Cert = parser.ReadCertificate(cert.GetRawCertData());
            var subjects = x509Cert.GetSubjectAlternativeNames();

            foreach (var subject in subjects)
            {
                if (subject is IList list)
                {
                    if (list.Count >= 2 && list[0] is int nameType && nameType == 2)
                    {
                        var dnsName = list[1]?.ToString();
                        if (dnsName != null)
                            yield return dnsName;
                    }
                }
            }
        }

        private static bool IsMatch(string dnsName, string? domain)
        {
            if (domain == null)
                return false;

            if (dnsName == domain)
                return true;

            if (dnsName[0] == '*')
                return domain.EndsWith(dnsName[1..]);

            return false;
        }

        private SocketsHttpHandler CreateSocketsHttpHandler()
        {
            return new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = System.Net.DecompressionMethods.None,
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                    var stream = new NetworkStream(socket, ownsSocket: true);

                    var requestContext = context.InitialRequestMessage.GetRequestContext();
                    if (requestContext.IsHttps == false)
                        return stream;

                    var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = requestContext.TlsSniPattern.Value,
                        RemoteCertificateValidationCallback = ValidateServerCertificate
                    }, cancellationToken);
                    return sslStream;

                    bool ValidateServerCertificate(
                        object sender,
                        X509Certificate? cert,
                        X509Chain? chain,
                        SslPolicyErrors errors)
                    {
                        if (errors == SslPolicyErrors.RemoteCertificateNameMismatch)
                        {
                            if (this.domainConfig.TlsIgnoreNameMismatch == true)
                            {
                                return true;
                            }

                            var domain = requestContext.Domain;
                            var dnsNames = ReadDnsNames(cert);
                            return dnsNames.Any(dns => IsMatch(dns, domain));
                        }

                        return errors == SslPolicyErrors.None;
                    }
                }
            };
        }

        public HttpClientHandler(DomainConfig domainConfig, IDomainResolver domainResolver)
        {
            this.domainConfig = domainConfig;
            this.domainResolver = domainResolver;
            InnerHandler = CreateSocketsHttpHandler();
        }
    }
}
