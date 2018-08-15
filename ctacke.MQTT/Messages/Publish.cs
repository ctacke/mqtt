using OpenNETCF;
using System;
using System.Text;

namespace ctacke.MQTT
{
    internal class Publish : Message
    {
        private VariableHeader<PublishHeaderData> m_header;
        private byte[] m_data;

        internal Publish(FixedHeader header, byte[] payload)
            : base(header)
        {
            var payloadLength = payload.Length;
            m_header = new VariableHeader<PublishHeaderData>();
            VariableHeader = m_header;

            // get topic length
            var topicLength = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(payload, 0));
            payloadLength -= 2;

            // get topic name
            m_header.HeaderData.TopicName = Encoding.UTF8.GetString(payload, 2, topicLength);
            payloadLength -= topicLength;

            if (header.QoS > 0)
            {
                // pull message ID
                m_header.HeaderData.MessageID = (ushort)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(payload, payload.Length - payloadLength));                 
                payloadLength -= 2;
            }

            m_data = new byte[payloadLength];
            Buffer.BlockCopy(payload, payload.Length - payloadLength, m_data, 0, m_data.Length);
        }

        public Publish(string topicName, byte[] messageData)
            : this(topicName, messageData, 0, QoS.FireAndForget, false)
        {
        }

        public Publish(string topicName, byte[] messageData, ushort messageID, QoS qos, bool retain)
            : base(MessageType.Publish, qos, retain, false)
        {
            m_header = new VariableHeader<PublishHeaderData>();
            VariableHeader = m_header;

            Validate
                .Begin()
                .IsNotNullOrEmpty(topicName)
                .ParameterIsNotNull(messageData, "messageData")
                .Check();

            m_header.HeaderData.TopicName = topicName;
            if (messageID != 0)
            {
                m_header.HeaderData.MessageID = messageID;
            }

            m_data = messageData;
        }

        public string Topic 
        {
            get
            {
                return m_header.HeaderData.TopicName;
            }
        }

        public override byte[] Payload
        {
            get { return m_data; }
        }
    }
}
