using System.Net.Http.Headers;

namespace fake_fastgithub
{
    public class HttpClient : HttpMessageInvoker
    {
        private readonly static ProductInfoHeaderValue userAgent =
            new(new ProductHeaderValue(nameof(fake_fastgithub), "1.0"));

        internal HttpClient(HttpClientHandler handler, bool disposeHandler)
            : base(handler, disposeHandler)
        { }

        public HttpClient(DomainConfig domainConfig, IDomainResolver domainResolver)
            : this(new HttpClientHandler(domainConfig, domainResolver), disposeHandler: true)
        { }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"HttpClient.SendAsync RequestUri: {request.RequestUri}");

            if (request.Headers.UserAgent.Contains(userAgent))
            {
                throw new Exception($"由于 {request.RequestUri} 实际指向了 {nameof(fake_fastgithub)} 自身，" +
                    $"{nameof(fake_fastgithub)} 已中断本次转发");
            }

            request.Headers.UserAgent.Add(userAgent);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
