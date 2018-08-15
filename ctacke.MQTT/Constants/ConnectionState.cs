#if MONO
using Output = System.Console;
#else
using Output = System.Diagnostics.Debug;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Diagnostics;

#if !WindowsCE
using System.Net.Security;
#endif

namespace ctacke.MQTT
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }
}
