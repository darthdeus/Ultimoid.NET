using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ultimoid.Lib.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1() {
            var data = new Datagram {
                Ack = 23,
                Seq = 44,
                Payload = new byte[] {0xDE, 0xAD, 0xBE, 0xEF}
            };

            byte[] serialized = Protocol.Serialize(data);
            var deserialized = Protocol.Deserialize(serialized);

            Assert.AreEqual(data.Ack, deserialized.Ack);
            Assert.AreEqual(data.Seq, deserialized.Seq);
            CollectionAssert.AreEqual(data.Payload, deserialized.Payload);
        }
    }
}
