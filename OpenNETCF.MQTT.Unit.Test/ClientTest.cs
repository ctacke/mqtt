﻿using OpenNETCF.MQTT;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Diagnostics;

namespace OpenNETCF.MQTT.Unit.Test
{
    // Could not load file or assembly 'System.Net.Sockets, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' 
    [TestClass()]
    public class ClientTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        public void ClientReceiveTest()
        {
            var receivedMessage = false;

            var client = new MQTTClient("test.mosquitto.org", 1883);
            client.MessageReceived += (topic, qos, payload) =>
            {
                Debug.WriteLine("RX: " + topic);
                receivedMessage = true;
            };

            var i = 0;
            client.Connect("OpenNETCF");
            while (!client.IsConnected)
            {
                Thread.Sleep(1000);

                if (i++ > 10) Assert.Fail();
            }

            Assert.IsTrue(client.IsConnected);
            client.Subscriptions.Add(new Subscription("OpenNETCF/#"));

            i = 0;
            while (true)
            {
                if (receivedMessage) break;

                Thread.Sleep(1000);
                client.Publish("OpenNETCF/Test", "Hello", QoS.FireAndForget, false);

                if (i++ > 10) break;
            }

            Assert.IsTrue(receivedMessage);
        }
    }
}
