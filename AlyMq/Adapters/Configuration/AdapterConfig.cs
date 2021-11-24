using System;
using System.Collections.Generic;
using System.Text;

namespace AlyMq.Adapters.Configuration
{
    public sealed class AdapterConfig
    {
        public static AdapterConfig Instance { get; private set; }

        private AdapterConfig() { }

        public static AdapterConfig Create() { Instance = new AdapterConfig(); return Instance; }

        public Guid Key { get; set; }

        public string Name { get; set; }

        public Address Address { get; set; }
    }
}
