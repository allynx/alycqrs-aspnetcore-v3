using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AlyMq.Brokers
{
    public interface IBrokerService
    {
        Task Start();
        Task Stop();
    }
}
