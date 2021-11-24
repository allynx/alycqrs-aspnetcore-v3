using System;
using System.Collections.Generic;
using System.Text;

namespace AlyMq
{
    /// <summary>
    /// message instruct struct.
    /// </summary>
    [Serializable]
    public struct Instruct 
    {
        public const int ReportBroker = 1;
        public const int PullBrokers = 2;
        public const int ReportProducer = 3;
        public const int PushConsumerToBroker = 4;
    }
}
