using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace OpenNETCF.MQTT
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
