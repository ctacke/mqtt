using OpenNETCF;
using System.Collections.Generic;

namespace ctacke.MQTT
{
    internal class FixedHeader
    {
        public FixedHeader()
        {
        }

        public bool Retain { get; set; }
        public QoS QoS { get; set; }
        public bool DuplicateDelivery { get; set; }
        public MessageType MessageType { get; set; }
        public int RemainingLength { get; set; }

        private const byte RetainBit = (1 << 0);
        private const byte DuplicateDeliveryBit = (byte)(1 << 3);

        public byte[] Serialize()
        {
            var data = new List<byte>(5);

            data.Add(0);

            // ---- message type ----
            data[0] |= (byte)(((byte)MessageType) << 4);

            // ---- duplicate ----
            if (DuplicateDelivery)
            {
                data[0] |= DuplicateDeliveryBit;
            }

            // ---- qos ----
            //var qos = (byte)(IPAddress.HostToNetworkOrder((int)QoS) << 1);
            var qos = (byte)(((int)QoS) << 1);
            data[0] |= qos;

            // ---- retain ----
            if (Retain)
            {
                data[0] |= RetainBit;
            }

            // ---- remaining length ----
            data.AddRange(EncodeRemainingLength(RemainingLength));

            return data.ToArray();
        }

        public static FixedHeader Deserialize(byte[] data)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(data, "data")
                .IsGreaterThanOrEqualTo(data.Length, 2)
                .Check();

            var header = new FixedHeader();

            // ---- message type ----
            header.MessageType = (MessageType)(data[0] >> 4);
            // ---- duplicate ----
            header.DuplicateDelivery = (data[0] & DuplicateDeliveryBit) == DuplicateDeliveryBit;
            // ---- qos ----
            var qos = (QoS)(( data[0] >> 1) & 3);
            header.QoS = qos;

            // ---- retain ----
            header.Retain = (data[0] & RetainBit) == RetainBit;

            // ---- remaining length ----
            header.RemainingLength = DecodeRemainingLength(data, 1);
            return header;
        }

        private byte[] EncodeRemainingLength(int length)
        {
            var data = new List<byte>(4);
            int digit;

            do
            {
                digit = length % 128;
                length = length / 128;

                if (length > 0)
                {
                    digit = digit | 0x80;
                }
                data.Add((byte)digit);
            } while (length > 0);

            return data.ToArray();
        }

        internal static int DecodeRemainingLength(IEnumerable<byte> data)
        {
            return DecodeRemainingLength(data, 0);
        }

        internal static int DecodeRemainingLength(IEnumerable<byte> data, int startOffset)
        {
            var value = 0;
            var factor = 1;
            var pos = 0;

            foreach (var b in data)
            {
                if (pos++ < startOffset) continue;

                value += (b & ~0x80) * factor;

                factor <<= 7;
            }

            return value;
        }
    }
}
