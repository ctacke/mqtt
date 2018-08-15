namespace ctacke.MQTT
{
    internal class ConnectAck : Message
    {
        private byte[] m_payload;

        public ConnectResult Result
        {
            get
            {
                return (ConnectResult)m_payload[1];
            }
        }

        internal ConnectAck(FixedHeader header, byte[] payload)
            : base(header)
        {
            m_payload = payload;
        }

        public override byte[] Payload
        {
            get { return m_payload; }
        }
    }
}
