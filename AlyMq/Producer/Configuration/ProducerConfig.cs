using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AlyMq.Producer.Configuration
{
    public sealed class ProducerConfig
    {
        public static ProducerConfig Instance { get; private set; }

        private ProducerConfig() { }

        public static ProducerConfig Create() { Instance = new ProducerConfig(); return Instance; }

        public Guid Key { get; set; }

        public string Name { get; set; }

        public Address Address { get; set; }

        public Address AdapterAddress { get; set; }
    }
}
