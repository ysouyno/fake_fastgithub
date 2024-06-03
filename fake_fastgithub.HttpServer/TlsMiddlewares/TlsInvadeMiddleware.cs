using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using System.Buffers;
using System.IO.Pipelines;

namespace fake_fastgithub.HttpServer.TlsMiddlewares
{
    sealed class TlsInvadeMiddleware
    {
        public async Task InvokeAsync(ConnectionDelegate next, ConnectionContext context)
        {
            if (await IsTlsConnectionAsync(context) == false)
            {
                if (context.Features.Get<ITlsConnectionFeature>() == null)
                {
                    context.Features.Set<ITlsConnectionFeature>(FakeTlsConnectionFeature.Instance);
                }
            }

            await next(context);
        }

        private static async Task<bool> IsTlsConnectionAsync(ConnectionContext context)
        {
            try
            {
                var result = await context.Transport.Input.ReadAtLeastAsync(2, context.ConnectionClosed);
                var state = IsTlsProtocol(result);
                context.Transport.Input.AdvanceTo(result.Buffer.Start);
                return state;
            }
            catch
            {
                return false;
            }

            static bool IsTlsProtocol(ReadResult result)
            {
                var reader = new SequenceReader<byte>(result.Buffer);
                return reader.TryRead(out var firstByte) &&
                    reader.TryRead(out var nextByte) &&
                    firstByte == 0x16 &&
                    nextByte == 0x3;
            }
        }
    }
}
