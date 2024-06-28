namespace fake_fastgithub
{
    public record ResponseConfig
    {
        public int StatusCode { get; init; } = 200;

        public string ContentType { get; init; } = "text/plain;charset=utf-8";

        public string? ContentValue { get; init; }
    }
}
