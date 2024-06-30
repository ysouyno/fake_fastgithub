namespace fake_fastgithub
{
    public static class ReverseProxyApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseReverseProxy(this IApplicationBuilder app)
        {
            var middleware = app.ApplicationServices.GetRequiredService<ReverseProxyMiddleware>();
            return app.Use(next => context => middleware.InvokeAsync(context, next));
        }
    }
}
