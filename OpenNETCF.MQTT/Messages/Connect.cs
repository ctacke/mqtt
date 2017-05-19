using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.MQTT
{
    internal class Connect : Message
    {
        private VariableHeader<ConnectHeaderData> m_header;
        
        public MQTTString UserName { get; private set; }
        public MQTTString Password { get; private set; }
        public MQTTString ClientIdentifier { get; private set; }

        // TODO:
        private MQTTString WillTopic { get; set; }
        private MQTTString WillMessage { get; set; }

        public Connect(string userName, string password, string clientIdentifier)
            : base(MessageType.Connect)
        {
            m_header = new VariableHeader<ConnectHeaderData>();
            VariableHeader = m_header;

            if(!userName.IsNullOrEmpty())
            {
                UserName = userName;
                m_header.HeaderData.HasUserName = true;
            }

            if(!password.IsNullOrEmpty())
            {
                Password = password;
                m_header.HeaderData.HasPassword = true;
            }

            Validate
                .Begin()
                .ParameterIsNotNull(clientIdentifier, "clientIdentifier")
                .IsGreaterThanOrEqualTo(clientIdentifier.Length, 1)
                .IsLessThanOrEqualTo(clientIdentifier.Length, 23)
                .Check();

            ClientIdentifier = clientIdentifier;
        }

        public override byte[] Payload
        {
            get 
            {
                var data = new List<byte>();

                data.AddRange(ClientIdentifier.Serialize());

                if (WillTopic != null)
                {
                    data.AddRange(WillTopic.Serialize());
                }
                if (WillMessage != null)
                {
                    data.AddRange(WillMessage.Serialize());
                }
                if (UserName != null)
                {
                    data.AddRange(UserName.Serialize());
                }
                if (Password != null)
                {
                    data.AddRange(Password.Serialize());
                }

                return data.ToArray();
            }        
        }
    }
}
