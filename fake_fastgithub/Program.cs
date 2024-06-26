namespace fake_fastgithub
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseBinaryPathContentRoot()
                .UseDefaultServiceProvider(c =>
                {
                    c.ValidateOnBuild = true;
                })
                .ConfigureAppConfiguration(c =>
                {
                    if (Directory.Exists("appsettings") == true)
                    {
                        foreach (var jsonFile in Directory.GetFiles("appsettings", "appsettings.*.json"))
                        {
                            c.AddJsonFile(jsonFile, true, true);
                        }
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseShutdownTimeout(TimeSpan.FromSeconds(2d));
                    webBuilder.UseKestrel(kestrel =>
                    {
                        kestrel.Limits.MaxRequestBodySize = null;
                        kestrel.ListenGithubSshProxy();
                    });
                });
        }
    }
}