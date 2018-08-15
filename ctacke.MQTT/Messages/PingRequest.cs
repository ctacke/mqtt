namespace ctacke.MQTT
{
    internal class PingRequest : Message
    {
        public PingRequest()
            : base(MessageType.PingRequest)
        {
            // NOTE: PingRequest has no variable header and no payload

            VariableHeader = null;
        }

        public override byte[] Payload
        {
            get { return null; }
        }
    }
}
