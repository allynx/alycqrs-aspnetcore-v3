using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AlyMq.Producers
{
    public interface IProducerService
    {
        Task Start();
        Task Stop();
    }
}
