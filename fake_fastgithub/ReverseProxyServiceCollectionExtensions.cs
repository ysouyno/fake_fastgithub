namespace fake_fastgithub
{
    public static class ReverseProxyServiceCollectionExtensions
    {
        public static IServiceCollection AddReverseProxy(this IServiceCollection services)
        {
            return services
                .AddMemoryCache()
                .AddHttpForwarder()
                .AddSingleton<CertService>();
        }
    }
}
