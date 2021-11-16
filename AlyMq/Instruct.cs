using System;
using System.Collections.Generic;
using System.Text;

namespace AlyMq
{
    /// <summary>
    /// message instruct enum.
    /// </summary>
    [Serializable]
    public struct Instruct 
    {
        public const int ReportBrokerTopics = 1;
        public const int PullBrokerByTopicKeys = 2;
        public const int PushBrokerFromAdapter = 3;
    }
}
