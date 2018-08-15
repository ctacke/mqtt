using OpenNETCF;
using System.Collections.Generic;

namespace ctacke.MQTT
{
    internal class Subscribe : Message
    {
        private VariableHeader<MessageIDHeaderData> m_header;
        private Subscription[] m_subscriptions;

        public Subscribe(Subscription[] subscriptions, ushort messageID)
            : base(MessageType.Subscribe, QoS.AcknowledgeDelivery, false, false)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(subscriptions, "subscriptions")
                .IsGreaterThanOrEqualTo(1, subscriptions.Length)
                .Check();

            m_header = new VariableHeader<MessageIDHeaderData>();
            m_header.HeaderData.MessageID = messageID;
            VariableHeader = m_header;

            m_subscriptions = subscriptions;
        }

        public override byte[] Payload
        {
            get 
            {
                var data = new List<byte>(m_subscriptions.Length * 3);

                foreach (var s in m_subscriptions)
                {
                    data.AddRange(s.Serialize());
                }

                return data.ToArray();
            }
        }
    }
}
