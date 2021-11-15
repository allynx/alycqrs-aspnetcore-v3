using System;
using System.Collections.Generic;
using System.Text;

namespace AlyMq.Consumer.Configuration
{
    public sealed class ConsumerConfig
    {
        public static ConsumerConfig Instance { get; private set; }

        private ConsumerConfig() { }

        public static ConsumerConfig Create() { Instance = new ConsumerConfig(); return Instance; }

        public Guid Key { get; set; }

        public string Name { get; set; }

        public Address Address { get; set; }

        public Address AdapterAddress { get; set; }
    }
}
