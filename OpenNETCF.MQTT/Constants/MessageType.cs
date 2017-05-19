using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.MQTT
{
    public enum MessageType
    {
        Reserved_0 = 0,
        Connect = 1,
        ConnectAck = 2,
        Publish = 3,
        PublishAck = 4,
        PublishReceive = 5,
        PublishRelease = 6,
        PublishComplete = 7,
        Subscribe = 8,
        SubscribeAck = 9,
        Unsubscribe = 10,
        UnsubscribeAck = 11,
        PingRequest = 12,
        PingResponse = 13,
        Disconnect = 14,
        Reserved_15 = 15
    }
}
