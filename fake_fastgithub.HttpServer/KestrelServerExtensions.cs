using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace fake_fastgithub
{
    public static class KestrelServerExtensions
    {
        public static void NoLimit(this KestrelServerOptions kestrel)
        {
            kestrel.Limits.MaxRequestBodySize = null;
            kestrel.Limits.MinResponseDataRate = null;
            kestrel.Limits.MinRequestBodyDataRate = null;
        }
    }
}
