using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlyMq.Consumer.Configuration
{
    public static class ConsumerConfigurationExtensions
    {
        /// <summary>
        /// 添加消费者配置
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMqConsumer(this IServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("consumerconfig.json", true, true)
              .Build();

            config.Bind("ConsumerConfig", ConsumerConfig.Create());

           return services.AddTransient<IConsumerService, DefaultConsumerService>();
        }
    }
}
