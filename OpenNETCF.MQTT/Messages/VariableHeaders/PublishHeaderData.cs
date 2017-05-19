using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace OpenNETCF.MQTT
{
    public class PublishHeaderData : HeaderData
    {
        private MQTTString m_topic;

        public MQTTString TopicName 
        {
            get { return m_topic; }
            set
            {
                m_topic = Uri.EscapeUriString(value);
            }
        }
        public ushort? MessageID { get; set; }

        public override byte[] Serialize()
        {
            var data = new List<byte>(TopicName.Value.Length);
            data.AddRange(TopicName.Serialize());

            if (MessageID.HasValue)
            {
                data.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(MessageID.Value)));
            }

            return data.ToArray();
        }
    }
}
