using System;
using System.Collections.Generic;
using System.Text;

namespace AlyMq
{
    [Serializable]
    public class Router
    {
        public Guid TopicKey { get; set; }
        public string TopicName { get; set; }
        public string TopicTags { get; set; }
        public int TopicQueueQuantity { get; set; }
        public Guid BrokerKey { get; set; }
        public string BrokerIp { get; set; }
        public int BrokerPort { get; set; }
    }
}
