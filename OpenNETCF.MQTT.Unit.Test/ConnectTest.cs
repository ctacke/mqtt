﻿using OpenNETCF.MQTT;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace OpenNETCF.MQTT.Unit.Test
{
    [TestClass()]
    public class ConnectTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        public void ConnectConstructorTest()
        {
            var connect = new Connect("OpenNETCF", "cloudCT1!", "ctacke211");

            var bytes = connect.Serialize();
        }
    }
}
