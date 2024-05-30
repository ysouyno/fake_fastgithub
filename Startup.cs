using Serilog;

namespace fake_fastgithub
{
    static class Startup
    {
        public static void ConfigureHost(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((hosting, logger) =>
            {
                var template = "{Timestamp:O} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                logger
                .ReadFrom.Configuration(hosting.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: template)
                .WriteTo.File(Path.Combine("fakelogs", @"log.txt"), rollingInterval: RollingInterval.Day, outputTemplate: template);
            });
        }
    }
}
