using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.MQTT
{
    internal class Disconnect : Message
    {
        public Disconnect()
            : base(MessageType.Disconnect)
        {
            // NOTE: Disconnect has no variable header and no payload

            VariableHeader = null;
        }

        public override byte[] Payload
        {
            get { return null; }
        }
    }
}
