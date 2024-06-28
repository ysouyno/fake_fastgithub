namespace fake_fastgithub
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConfiguration().Bind(Configuration.GetSection(nameof(fake_fastgithub)));
            services.AddDnsServer();
            services.AddReverseProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
