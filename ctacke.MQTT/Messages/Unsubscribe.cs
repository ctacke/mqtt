﻿using OpenNETCF;
using System.Collections.Generic;

namespace ctacke.MQTT
{
    internal class Unsubscribe : Message
    {
        private VariableHeader<MessageIDHeaderData> m_header;
        private string[] m_topics;

        public Unsubscribe(string[] topics, ushort messageID)
            : base(MessageType.Unsubscribe, QoS.AcknowledgeDelivery, false, false)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(topics, "topics")
                .IsGreaterThanOrEqualTo(1, topics.Length)
                .Check();

            m_header = new VariableHeader<MessageIDHeaderData>();
            m_header.HeaderData.MessageID = messageID;
            VariableHeader = m_header;

            m_topics = topics;
        }

        public override byte[] Payload
        {
            get 
            {
                var data = new List<byte>(512);

                foreach (var s in m_topics)
                {
                    data.AddRange(((MQTTString)s).Serialize());
                }

                return data.ToArray();
            }
        }
    }
}
