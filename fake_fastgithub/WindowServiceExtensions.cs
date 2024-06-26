namespace fake_fastgithub
{
    static class WindowServiceExtensions
    {
        /// <summary>
        /// 运行 dnscrypt-proxy.exe 时可以找到 dnscrypt-proxy.toml 文件
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseBinaryPathContentRoot(this IHostBuilder hostBuilder)
        {
            var contentRoot = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            if (contentRoot != null)
            {
                Environment.CurrentDirectory = contentRoot;
                hostBuilder.UseContentRoot(contentRoot);
            }
            return hostBuilder;
        }
    }
}
