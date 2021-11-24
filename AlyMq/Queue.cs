using Nancy.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace AlyMq
{
    [Serializable]
    public class Queue
    {
        [NonSerialized]
        private ConcurrentQueue<Msg> _msgQueues = new ConcurrentQueue<Msg>();

        public Guid Key { get; set; }

        public Guid TopicKey { get; set; }

        public string Name { get; set; }

        public DateTime CreateOn { get; set; }

        public int MsgQueuesQuantity { get { return _msgQueues.Count; } }

        [XmlIgnore]
        [ScriptIgnore]
        public ConcurrentQueue<Msg> MsgQueues { get { return _msgQueues; } set { _msgQueues = value; } }
    }
}
