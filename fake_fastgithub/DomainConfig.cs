namespace fake_fastgithub
{
    public record DomainConfig
    {
        /// <summary>
        /// 是否发送 SNI
        /// </summary>
        public bool TlsSni { get; init; }

        /// <summary>
        /// 自定义 SNI 值的表达式
        /// </summary>
        public string? TlsSniPattern { get; init; }

        /// <summary>
        /// 是否忽略服务器证书域名不匹配
        /// 当不发送 SNI 时服务器可能发回域名不匹配的证书
        /// </summary>
        public bool TlsIgnoreNameMismatch { get; init; }

        /// <summary>
        /// 使用的 ip 地址
        /// </summary>
        public string? IPAddress { get; init; }

        /// <summary>
        /// 请求超时时长
        /// </summary>
        public TimeSpan? Timeout { get; init; }

        /// <summary>
        /// 格式为相对或绝对 uri
        /// </summary>
        public Uri? Destination { get; init; }

        /// <summary>
        /// 自定义响应
        /// </summary>
        public ResponseConfig? Response { get; init; }

        /// <summary>
        /// 获取 TlsSniPattern
        /// </summary>
        /// <returns></returns>
        public TlsSniPattern GetTlsSniPattern()
        {
            return new TlsSniPattern(); // TODO
        }
    }
}
