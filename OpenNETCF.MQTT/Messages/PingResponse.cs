using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.MQTT
{
    internal class PingResponse : Message
    {
        internal PingResponse(FixedHeader header, byte[] payload)
            : base(header.MessageType)
        {
            // NOTE: PingResponse has no variable header and no payload

            VariableHeader = null;
        }

        public override byte[] Payload
        {
            get { return null; }
        }
    }
}
