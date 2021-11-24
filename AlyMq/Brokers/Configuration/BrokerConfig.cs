using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AlyMq.Brokers.Configuration
{
    public sealed class BrokerConfig
    {

        public static BrokerConfig Instance { get; private set; }

        private BrokerConfig() { }

        public static BrokerConfig Create() { Instance = new BrokerConfig(); return Instance; }

        public Guid Key { get; set; }

        public string Name { get; set; }

        public Address Address { get; set; }

        public Address AdapterAddress { get; set; }
    }
}
