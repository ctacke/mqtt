using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace OpenNETCF.MQTT
{
    public class ConnectHeaderData : HeaderData
    {
        public const short DefaultKeepAlive = 0x3c;

        public MQTTString ProtocolName { get; set; }
        public byte ProtocolVersion { get; set; }

        public bool CleanSession { get; set; }
        public bool Will { get; set; }
        public QoS WillQoS { get; set; }
        public bool WillRetain { get; set; }
        public bool HasUserName { get; set; }
        public bool HasPassword { get; set; }
        public short KeepAliveSeconds { get; set; }

        public ConnectHeaderData()
        {
            ProtocolName = "MQIsdp";
            ProtocolVersion = 0x03;

            CleanSession = true;
            KeepAliveSeconds = DefaultKeepAlive;
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();

            data.AddRange(ProtocolName.Serialize());
            data.Add(ProtocolVersion);

            byte flags = 0;

            if (CleanSession)
            {
                flags |= (byte)(1 << 1);
            }

            if (Will)
            {
                flags |= (byte)(1 << 2); // will
                flags |= (byte)(((int)WillQoS) << 3); // will qos
                if (WillRetain)
                {
                    flags |= (byte)(1 << 5); // will retain
                }
            }

            if(HasPassword)
            {
                flags |= (byte)(1 << 6);
            }

            if (HasUserName)
            {
                flags |= (byte)(1 << 7);
            }

            data.Add(flags);

            data.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(KeepAliveSeconds)));

            return data.ToArray();
        }
    }
}
