namespace fake_fastgithub
{
    public interface IHttpClientFactory
    {
        HttpClient CreateHttpClient(DomainConfig domainConfig);
    }
}
