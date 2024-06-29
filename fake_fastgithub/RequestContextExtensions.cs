namespace fake_fastgithub
{
    static class RequestContextExtensions
    {
        private static readonly HttpRequestOptionsKey<RequestContext> key = new(nameof(RequestContext));

        public static void SetRequestContext(this HttpRequestMessage httpRequestMessage, RequestContext requestContext)
        {
            httpRequestMessage.Options.Set(key, requestContext);
        }

        public static RequestContext GetRequestContext(this HttpRequestMessage httpRequestMessage)
        {
            return httpRequestMessage.Options.TryGetValue(key, out var requestContext)
                ? requestContext
                : throw new InvalidOperationException($"请先调用 {nameof(SetRequestContext)}");
        }
    }
}
