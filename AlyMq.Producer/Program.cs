using AlyMq.Producers.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AlyMq.Producers
{
    class Program
    {
        static async Task Main()
        {
            using ServiceProvider serviceProvider = new ServiceCollection().ConfigureServices().BuildServiceProvider();
            IProducerService service = serviceProvider.GetService<IProducerService>();

            await service.Start();

            Console.Read();
        }


    }
    internal static class Startup
    {
        internal static IServiceCollection ConfigureServices(this ServiceCollection services)
        {
            return services
                 .AddAlyMq()
                 .AddMqProducter();
        }
    }
}

