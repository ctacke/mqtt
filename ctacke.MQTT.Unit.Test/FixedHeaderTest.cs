using ctacke.MQTT;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ctacke.MQTT.Unit.Test
{
    [TestClass()]
    public class FixedHeaderTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        public void DeserializationTest_1()
        {
            var bytes = new byte[] { 0x20, 0x02 };
            var header = FixedHeader.Deserialize(bytes);

            Assert.AreEqual(MessageType.ConnectAck, header.MessageType);
            Assert.AreEqual(2, header.RemainingLength);
        }
    }
}
