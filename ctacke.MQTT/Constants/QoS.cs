using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ctacke.MQTT
{
    public enum QoS
    {
        FireAndForget = 0,
        AcknowledgeDelivery = 1,
        AssureDelivery = 2,
        Reserved = 3
    }
}
