using Nancy.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace AlyMq
{
    [Serializable]
    public class Topic
    {

        public Guid Key { get; set; }

        public Guid BrokerKey { get; set; }

        public string Name { get; set; }

        public string Tag { get; set; }

        public HashSet<Queue> Queues { get; set; } 

        public DateTime CreateOn { get; set; } = DateTime.Now;

        public override bool Equals(object obj)
        {
            var other = obj as Topic;
            return obj == this ||
                other.Key == Key &&
                other.Name == Name &&
                other.Tag == Tag &&
                other.BrokerKey == BrokerKey &&
                other.CreateOn == CreateOn;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode() ^
                BrokerKey.GetHashCode() ^
                Name.GetHashCode() ^
                Tag.GetHashCode() ^
                CreateOn.GetHashCode();
        }
     }
}
