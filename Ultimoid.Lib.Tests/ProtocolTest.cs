using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ultimoid.Lib.Tests {
    [TestClass]
    public class ProtocolTest {
        [TestMethod]
        public void TestMethod1() {
            var data = new Datagram {
                Ack = 23,
                Seq = 44,
                AckField = 423098,
                MessageId = 3,
                Payload = new byte[] {0xDE, 0xAD, 0xBE, 0xEF}
            };

            byte[] serialized = Protocol.Serialize(data);
            var deserialized = Protocol.Deserialize(serialized);

            Assert.AreEqual(data.Ack, deserialized.Ack);
            Assert.AreEqual(data.Seq, deserialized.Seq);
            Assert.AreEqual(data.AckField, deserialized.AckField);
            Assert.AreEqual(data.MessageId, deserialized.MessageId);
            CollectionAssert.AreEqual(data.Payload, deserialized.Payload);
        }
    }
}