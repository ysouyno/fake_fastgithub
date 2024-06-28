using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace fake_fastgithub
{
    public static class ServiceCollectionExtensions
    {
        public static OptionsBuilder<FastGithubOptions> AddConfiguration(this IServiceCollection services)
        {
            services.TryAddSingleton<FastGithubOptions>();
            return services.AddOptions<FastGithubOptions>();
        }
    }
}
