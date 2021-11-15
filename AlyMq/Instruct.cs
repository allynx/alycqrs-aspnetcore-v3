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
        /// <summary>
        ///A value indicates report topics and broker relationship.
        /// </summary>
        public const int ReportBrokerTopics = 1;

        /// <summary>
        /// A value indicates pull topics and broker relationship.
        /// </summary>
        public const int PullBrokerTopics = 2;

        /// <summary>
        /// A value indicates push topics and broker relationship.
        /// </summary>
        public const int PushBrokerTopics = 3;
    }
}
