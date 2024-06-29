using Yarp.ReverseProxy.Forwarder;

namespace fake_fastgithub
{
    sealed class ReverseProxyMiddleware
    {
        private readonly IHttpForwarder httpForwarder;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<ReverseProxyMiddleware> logger;

        public ReverseProxyMiddleware(IHttpForwarder httpForwarder,
            IHttpClientFactory httpClientFactory,
            FastGithubConfig fastGithubConfig,
            ILogger<ReverseProxyMiddleware> logger)
        {
            this.httpForwarder = httpForwarder;
            this.httpClientFactory = httpClientFactory;
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        private string GetDestinationPrefix(string scheme, string host, Uri? destination)
        {
            var defaultValue = $"{scheme}://{host}/";
            if (destination == null)
            {
                return defaultValue;
            }

            var baseUri = new Uri(defaultValue);
            var result = new Uri(baseUri, destination).ToString();
            logger.LogInformation($"[{defaultValue} <-> {result}]");
            return result;
        }

        private static async Task HandleErrorAsync(HttpContext context, ForwarderError error)
        {
            if (error == ForwarderError.None)
                return;

            var errorFeature = context.GetForwarderErrorFeature();
            if (errorFeature == null || context.Response.HasStarted)
                return;

            await context.Response.WriteAsJsonAsync(new
            {
                error = error.ToString(),
                message = errorFeature.Exception?.Message
            });
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var host = context.Request.Host.Host;
            Console.WriteLine($"ReverseProxyMiddleware.InvokeAsync Host: {host}");
            if (fastGithubConfig.TryGetDomainConfig(host, out var domainConfig) == false)
            {
                await next(context);
            }
            else if (domainConfig.Response != null)
            {
                context.Response.StatusCode = domainConfig.Response.StatusCode;
                context.Response.ContentType = domainConfig.Response.ContentType;
                if (domainConfig.Response.ContentValue != null)
                {
                    await context.Response.WriteAsync(domainConfig.Response.ContentValue);
                }
            }
            else
            {
                var scheme = context.Request.Scheme;
                var destinationPrefix = GetDestinationPrefix(scheme, host, domainConfig.Destination);
                var httpClient = httpClientFactory.CreateHttpClient(domainConfig);
                var error = await httpForwarder.SendAsync(context, destinationPrefix, httpClient);
                await HandleErrorAsync(context, error);
            }
        }
    }
}
