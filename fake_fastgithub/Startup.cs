namespace fake_fastgithub
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddReverseProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
