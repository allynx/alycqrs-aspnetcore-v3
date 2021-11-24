using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using AlyMq.Consumers.Configuration;

namespace AlyMq.Consumers
{
    class Program
    {
        static async Task  Main(string[] args)
        {
            using (ServiceProvider serviceProvider = new ServiceCollection().ConfigureServices().BuildServiceProvider())
            {
                IConsumerService service = serviceProvider.GetService<IConsumerService>();

                await service.Start();

                Console.Read();
            }
        }
    }

    internal static class Startup
    {
        internal static IServiceCollection ConfigureServices(this ServiceCollection services)
        {
            return services
                 .AddAlyMq()
                 .AddMqConsumer();
        }
    }
}
