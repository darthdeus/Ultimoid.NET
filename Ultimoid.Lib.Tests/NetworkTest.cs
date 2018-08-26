using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ultimoid.Lib.Tests {
    [TestClass]
    public class NetworkTest {
        [TestMethod]
        public void TestUpdateAckFields() {
            var sched = new Scheduler();
            var network = new NetworkManager(sched, new FakeUdpClient());

            network.UpdateSendAckFields(new Datagram(1, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 1uL);
            Assert.AreEqual(network.CurrentAckField, 0b0000_0000_0000_0000u);

            network.UpdateSendAckFields(new Datagram(2, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 2uL);
            Assert.AreEqual(network.CurrentAckField, 0b0000_0000_0000_0001u);

            network.UpdateSendAckFields(new Datagram(3, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 3uL);
            Assert.AreEqual(network.CurrentAckField, 0b0000_0000_0000_0011u);

            network.UpdateSendAckFields(new Datagram(5, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 5uL);
            Assert.AreEqual(network.CurrentAckField, 0b0000_0000_0000_1110u);

            network.UpdateSendAckFields(new Datagram(15, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 15uL);
            Assert.AreEqual(network.CurrentAckField, 0b0011_1010_0000_0000u);

            network.UpdateSendAckFields(new Datagram(31, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 31uL);
            Assert.AreEqual(network.CurrentAckField, 0b0011_1010_0000_0000_1000_0000_0000_0000u);

            network.UpdateSendAckFields(new Datagram(34, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 34uL);
            Assert.AreEqual(network.CurrentAckField, 0b1101_0000_0000_0100_0000_0000_0000_0100u);

            network.UpdateSendAckFields(new Datagram(34, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 34uL);
            Assert.AreEqual(network.CurrentAckField, 0b1101_0000_0000_0100_0000_0000_0000_0100u);

            network.SendUnreliable(new IPEndPoint(IPAddress.Broadcast, 0), new byte[] { });

            network.UpdateSendAckFields(new Datagram(128, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 1uL);
            Assert.AreEqual(network.CurrentAck, 128uL);
            Assert.AreEqual(network.CurrentAckField, 0u);

            network.UpdateSendAckFields(new Datagram(131, 0, 0, 0, new byte[] {0x00}));

            Assert.AreEqual(network.CurrentSeq, 1uL);
            Assert.AreEqual(network.CurrentAck, 131uL);
            Assert.AreEqual(network.CurrentAckField, 0b0100u);
        }
    }
}