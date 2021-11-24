using System;
using System.Collections.Generic;
using System.Text;

namespace AlyMq.Producers
{
    [Serializable]
    public class Producer
    {
        public Guid Key { get; set; }

        public string Name { get; set; }

        public string Ip { get; set; }

        public int Port { get; set; }

        public int Backlog { get; set; }

        public DateTime CreateOn { get; set; }

        public DateTime PulseOn { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as Producer;
            PulseOn = other.PulseOn;
            return obj == this ||
                other.Key == Key &&
                other.Name == Name &&
                other.Ip == Ip &&
                other.Port == Port &&
                other.Backlog == Backlog &&
                other.CreateOn == CreateOn;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode() ^
                Name.GetHashCode() ^
                Ip.GetHashCode() ^
                Port.GetHashCode() ^
                Backlog.GetHashCode() ^
                CreateOn.GetHashCode();
        }
    }
}
