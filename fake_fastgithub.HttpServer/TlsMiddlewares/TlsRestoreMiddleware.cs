using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace fake_fastgithub.HttpServer.TlsMiddlewares
{
    sealed class TlsRestoreMiddleware
    {
        public async Task InvokeAsync(ConnectionDelegate next, ConnectionContext context)
        {
            if (context.Features.Get<ITlsConnectionFeature>() == FakeTlsConnectionFeature.Instance)
            {
                context.Features.Set<ITlsConnectionFeature>(null);
            }
            await next(context);
        }
    }
}
