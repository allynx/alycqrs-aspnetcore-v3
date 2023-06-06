using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlyMq.Adapters.Configuration
{
    public static class AdapterConfigurationExtensions
    {
        public static IServiceCollection AddMqAdapter(this IServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("adapterconfig.json", true, true)
                .Build();

            config.Bind("AdapterConfig", AdapterConfig.Create());

            return services.AddTransient<IAdapterService, DefaultAdapterService>();
        }
    }
}
