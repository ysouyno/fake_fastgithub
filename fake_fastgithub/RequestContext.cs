namespace fake_fastgithub
{
    sealed class RequestContext
    {
        public bool IsHttps { get; set; }
        public string? Domain { get; set; }
        public TlsSniPattern TlsSniPattern { get; set; }
    }
}
