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
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
#endif

[assembly: InternalsVisibleTo("OpenNETCF.MQTT.Unit.Test")]

namespace OpenNETCF.MQTT
{
    public delegate void PublicationReceivedHandler(string topic, QoS qos, byte[] payload);

    public class MQTTClient : DisposableBase
    {
        public event PublicationReceivedHandler MessageReceived;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public const int DefaultPort = 1883;

        private int DefaultTimeout = 120000;
        private int DefaultPingPeriod = 60000;
        private int DefaultReconnectPeriod = 5000;

        private TcpClient m_client;
        private Stream m_stream;
        private Timer m_pingTimer;
        private Timer m_reconnectTimer;
        private bool m_shouldReconnect;
        private ConnectionState m_state;
        private ushort m_currentMessageID;
        private object m_syncRoot = new object();
        private string m_lastUserName;
        private string m_lastPassword;
        private string m_lastClientIdentifier;
        private bool m_reconnecting;
        private Task m_rxTask;
        private CancellationTokenSource m_rxCancelToken;

        private CircularBuffer<Message> m_messageQueue = new CircularBuffer<Message>(100);

        public bool UseSSL { get; private set; }
        public string SSLTargetHost { get; private set; }
        public string BrokerHostName { get; private set; }
        public int BrokerPort { get; private set; }
        public int ReconnectPeriod { get; set; }
        public bool TracingEnabled { get; set; }
        public SubscriptionCollection Subscriptions { get; private set; }

        public MQTTClient(string brokerHostName)
            : this(brokerHostName, DefaultPort)
        {
        }

        public MQTTClient(string brokerHostName, int brokerPort)
            : this(brokerHostName, brokerPort, false, null)
        {
        }

        public MQTTClient(string brokerHostName, int brokerPort, bool useSSL, string sslTargetHost)
        {
            TracingEnabled = false;

            UseSSL = useSSL;
            SSLTargetHost = sslTargetHost;
            ReconnectPeriod = DefaultReconnectPeriod;

            BrokerHostName = brokerHostName;
            BrokerPort = brokerPort;

            ConnectionState = ConnectionState.Disconnected;

            Subscriptions = new SubscriptionCollection();
            Subscriptions.SubscriptionAdded += Subscriptions_SubscriptionAdded;
            Subscriptions.SubscriptionRemoved += Subscriptions_SubscriptionRemoved;
        }

        protected override void ReleaseManagedResources()
        {
            m_messageQueue.Clear();
            Disconnect();
        }

        private void Subscriptions_SubscriptionRemoved(object sender, GenericEventArgs<string> e)
        {
            var unsub = new Unsubscribe(new string[] { e.Value }, GetNextMessageID());
            Send(unsub);
        }

        private void Subscriptions_SubscriptionAdded(object sender, GenericEventArgs<Subscription> e)
        {
            var subs = new Subscribe(
                new Subscription[]
                {
                    e.Value
                },
                GetNextMessageID()
                );

            Send(subs);
        }

        private ushort GetNextMessageID()
        {
            lock (m_syncRoot)
            {
                if (m_currentMessageID == ushort.MaxValue)
                {
                    m_currentMessageID = 0;
                }

                m_currentMessageID++;

                return m_currentMessageID;
            }
        }

        public ConnectionState ConnectionState
        {
            get { return m_state; }
            private set
            {
                if (TracingEnabled)
                {
                    Output.WriteLine("MQTT State: " + value.ToString());
                }

                if (value == m_state) return;

                m_state = value;

                if (m_reconnectTimer == null)
                {
                    m_reconnectTimer = new Timer(ReconnectProc, null, Timeout.Infinite, Timeout.Infinite);
                }
                else
                {
                    switch (ConnectionState)
                    {
                        case MQTT.ConnectionState.Disconnected:
                            m_reconnectTimer.Change(ReconnectPeriod, ReconnectPeriod);
                            break;
                        default:
                            m_reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            break;
                    }
                }
            }
        }

        public string ClientIdentifier
        {
            get { return m_lastClientIdentifier; }
        }

        public async Task ConnectAsync(string clientID, string userName = null, string password = null)
        {
            if (await ConnectAsync(BrokerHostName, BrokerPort))
            {
                var connectMessage = new Connect(userName, password, clientID);
                await SendAsync(connectMessage);
            }
        }

        public void Connect(string clientID, string userName = null, string password = null)
        {
            AsyncHelper.RunSync(async () => await ConnectAsync(clientID, userName, password));
        }

        private bool Connect(string hostName, int port)
        {
            return AsyncHelper.RunSync(async () => await ConnectAsync(hostName, port));
        }

        private async Task<bool> ConnectAsync(string hostName, int port)
        {
            if (IsConnected) throw new Exception("Already connected");

            ConnectionState = MQTT.ConnectionState.Connecting;

            m_shouldReconnect = true;
            
            // create the client and connect
            m_client = new TcpClient();
            m_client.SendTimeout = DefaultTimeout;
            m_client.ReceiveTimeout = DefaultTimeout;

            try
            {
                await m_client.ConnectAsync(BrokerHostName, BrokerPort);
            }
            catch (Exception ex)
            {
                if (TracingEnabled)
                {
                    Output.WriteLine("Exception attempting to connect to MQTT Broker: " + ex.Message);
                }
                ConnectionState = MQTT.ConnectionState.Disconnected;
                return false;
            }

            var stream = m_client.GetStream();

            if (UseSSL)
            {
                throw new NotSupportedException();
                /*
                var ssl = new SslStream(stream, false, new RemoteCertificateValidationCallback(CertValidationProc));

                // TODO: allow local certificates
                ssl.AuthenticateAsClient(SSLTargetHost);

                stream = ssl;
                */
            }

            m_stream = stream;

            m_rxCancelToken = new CancellationTokenSource();

            if (m_rxTask != null)
            {
                m_rxCancelToken.Cancel();
                try
                {
                    m_rxTask.Wait();
                }
                catch { /* NOP from our cancellation */ }
                m_rxCancelToken.Dispose();
            }

            m_rxTask = Task.Run(() => RxThreadProc(), m_rxCancelToken.Token);

            return true;
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            m_shouldReconnect = false;
            IsConnected = false;
            Send(new Disconnect());

            if (m_rxTask != null)
            {
                m_rxCancelToken.Cancel();
                try
                {
                    m_rxTask.Wait();
                }
                catch { /* NOP from our cancellation */ }
                m_rxCancelToken.Dispose();
            }

            m_client.Dispose();
        }

        public bool IsConnected 
        {
            get { return ConnectionState == MQTT.ConnectionState.Connected; }
            private set
            {
                if (value)
                {
                    ConnectionState = MQTT.ConnectionState.Connected;

                    Connected.Fire(this, EventArgs.Empty);

                    // SEND ANY QUEUED ITEMS
                    while (m_messageQueue.Count > 0)
                    {
                        Send(m_messageQueue.Dequeue());
                        if (!IsConnected) break;
                    }
                }
                else
                {
                    ConnectionState = MQTT.ConnectionState.Disconnected;

                    Disconnected.Fire(this, EventArgs.Empty);
                }
            }
        }

        private int PingPeriod
        {
            get
            {
                if (DefaultTimeout < DefaultPingPeriod)
                {
                    return DefaultTimeout - 5000;
                }
                else
                {
                    return DefaultPingPeriod;
                }
            }
        }

        private void PingTimerProc(object state)
        {
            if (!IsConnected) return;

            var ping = new PingRequest();
            Send(ping);
        }

        private async void RxThreadProc()
        {
            try
            {
                if (IsConnected)
                {
#if !WindowsCE
                    m_stream.ReadTimeout = DefaultTimeout;
#endif
                }
            }
            catch (ObjectDisposedException)
            {
                IsConnected = false;
            }

            m_rxCancelToken.Token.ThrowIfCancellationRequested();


            // start a ping timer on a period less than the above timeout (else we'll timeout and exit)
            m_pingTimer = new Timer(PingTimerProc, null, PingPeriod, PingPeriod); 
            
            List<byte> header = new List<byte>(4);

            while((ConnectionState == ConnectionState.Connecting) || (ConnectionState == ConnectionState.Connected))
            {
                if (m_rxCancelToken.Token.IsCancellationRequested)
                {
                    m_rxCancelToken.Token.ThrowIfCancellationRequested();
                }

                header.Clear();

                // the first byte gives us the type
                try
                {
                    var byte0 = m_stream.ReadByte();
                    if (byte0 == -1)
                    {
                        await Task.Delay(500);
                        continue;
                    }

                    //var messageType = (MessageType)(byte0 >> 4);
                    header.Add((byte)byte0);

                    byte lengthByte;
                    // now pull the "remaining length"
                    do
                    {
                        lengthByte = (byte)m_stream.ReadByte();
                        header.Add(lengthByte);
                    } while ((lengthByte & 0x80) != 0);

                    var length = FixedHeader.DecodeRemainingLength(header, 1);

                    byte[] buffer = null;
                    int read = 0;

                    if (length > 0)
                    {
                        // and pull the payload
                        buffer = new byte[length];
                        do
                        {
                            read += m_stream.Read(buffer, read, length - read);
                        } while (read < length);
                    }

                    // deserialize and dispatch
                    DeserializeAndDispatchMessage(header.ToArray(), buffer);
                }
                catch (Exception ex)
                {
                    // happens during hang up, maybe on error
                    // needs testing
                    if (TracingEnabled)
                    {
                        Output.WriteLine("!!! Exception in MQTTBroker.RxProc\r\n" + ex.Message);
                    }
                    //if (Debugger.IsAttached) Debugger.Break();
                    IsConnected = false;
                }
            }

            m_pingTimer.Dispose();

        }

        private void ReconnectProc(object state)
        {
            DoReconnect();
        }

        private void DoReconnect()
        {
            switch (ConnectionState)
            {
                case MQTT.ConnectionState.Connected:
                case MQTT.ConnectionState.Connecting:
                    return;
            }

            if (m_shouldReconnect)
            {
                // make sure we don't ask more than once
                if (m_reconnecting) return;
                m_reconnecting = true;

                if (TracingEnabled)
                {
                    Output.WriteLine("MQTTBrokerProxy: Queueing up an auto-reconnect...");
                }
                try
                {
                    if (TracingEnabled)
                    {
                        Output.WriteLine("MQTTBrokerProxy: Reconnecting...");
                    }

                    Connect(BrokerHostName, BrokerPort);

                    if (ConnectionState == MQTT.ConnectionState.Connecting)
                    {
                        Send(new Connect(m_lastUserName, m_lastPassword, m_lastClientIdentifier));
                    }

                    m_reconnecting = false;
                }
                catch
                {
                    m_reconnecting = false;
                }
            }
        }

        private Message DeserializeAndDispatchMessage(byte[] headerdata, byte[] payload)
        {
            var header = FixedHeader.Deserialize(headerdata);

            if (TracingEnabled)
            {
                Output.WriteLine("Received: " + header.MessageType.ToString());
            }

            switch (header.MessageType)
            {
                case MessageType.ConnectAck:
                    var connect = new ConnectAck(header, payload);
                    IsConnected = true;
                    return connect;
                case MessageType.PingResponse:
                    return new PingResponse(header, payload);
                case MessageType.SubscribeAck:
                    break;
                case MessageType.Publish:
                    var pub = new Publish(header, payload);
                    var handler = MessageReceived;
                    if (handler != null)
                    {
                        handler(pub.Topic, pub.QoS, pub.Payload);
                    }
                    break;
                case MessageType.PublishAck:
                    // TODO: handle this
                    break;
                case MessageType.Connect:
                    // server connecting to us
                    throw new NotSupportedException();
                default:
                    throw new NotSupportedException();
            }

            return null;
        }

#if !WindowsCE
        public bool CertValidationProc(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // TODO: check for errors

            // allow all (even unauthenticated) servers
            return true;
        }
#endif

        internal void Send(Message message)
        {
            AsyncHelper.RunSync(async () => await SendAsync(message));
        }

        internal async Task SendAsync(Message message)
        {
            if ((!IsConnected) && !(message is Connect))
            {
                // queue these up for delivery after (re)connect
                m_messageQueue.Enqueue(message);
                return;
            }

            if (message is Connect)
            {
                m_lastUserName = (message as Connect).UserName;
                m_lastPassword = (message as Connect).Password;
                m_lastClientIdentifier = (message as Connect).ClientIdentifier;
            }

            byte[] data = null;

            try
            {
                data = message.Serialize();
            }
            catch(Exception ex)
            {
                if (TracingEnabled)
                {
                    Debug.WriteLine("MQTTBrokerProxy.Send: Serialization exception " + ex.Message);
                }
                if (Debugger.IsAttached) Debugger.Break();

                return;
            }

            try
            {
                await m_stream.WriteAsync(data, 0, data.Length);
                m_stream.Flush();
            }
            catch(Exception ex)
            {
                if (TracingEnabled)
                {
                    Debug.WriteLine("MQTTBrokerProxy.Send: stream write exception " + ex.Message);
                }
                IsConnected = false;
                //if (Debugger.IsAttached) Debugger.Break();

                // TODO: should we reconnect on all exceptions, or just some?
                //DoReconnect();
            }

            // TODO: queue this and look for response
        }

        public void Publish(string topic, string data, QoS qos, bool retain)
        {
            AsyncHelper.RunSync(async () => await PublishAsync(topic, data, qos, retain));
        }

        public void Publish(string topic, byte[] data, QoS qos, bool retain)
        {
            AsyncHelper.RunSync(async () => await PublishAsync(topic, data, qos, retain));
        }

        public async Task PublishAsync(string topic, string data, QoS qos, bool retain)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            await PublishAsync(topic, bytes, qos, retain);
        }

        public async Task PublishAsync(string topic, byte[] data, QoS qos, bool retain)
        {
            Publish publish;

            if (qos == QoS.FireAndForget)
            {
                publish = new Publish(topic, data);
            }
            else
            {
                var messageID = GetNextMessageID();
                publish = new Publish(topic, data, messageID, qos, retain);
            }

            await SendAsync(publish);
        }
    }
}
