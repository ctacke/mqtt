using OpenNETCF;
using System;
using System.Net;

namespace ctacke.MQTT
{
    public class MessageIDHeaderData : HeaderData
    {
        private ushort m_id;

        public ushort MessageID 
        {
            get { return m_id; }
            set
            {
                Validate
                    .Begin()
                    .AreNotEqual(value, 0)
                    .Check();

                m_id = value;
            }
        }

        public override byte[] Serialize()
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)MessageID));
        }
    }
}
