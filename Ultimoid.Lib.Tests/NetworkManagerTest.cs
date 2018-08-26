using System;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ultimoid.Lib.Tests {
    [TestClass]
    public class NetworkManagerTest {
        [TestMethod]
        public void TestTest() {
            var scheduler = new Scheduler();
            var client = new FakeUdpClient();
            var manager = new NetworkManager(scheduler, client);

            {
                var payload = new byte[] {0xCA, 0xFE};
                Assert.AreEqual(1u, manager.CurrentMessageId);
                Assert.AreEqual(1u, manager.CurrentSeq);
                manager.SendUnreliable(new IPEndPoint(IPAddress.Any, 0), payload);

                Assert.AreEqual(1, client.Sent.Count);
                Assert.AreEqual(2u, manager.CurrentMessageId);
                Assert.AreEqual(2u, manager.CurrentSeq);

                var sentDatagram = Protocol.Deserialize(client.Sent[0].Item1);

                Assert.AreEqual(1u, sentDatagram.MessageId);
                Assert.AreEqual(0u, sentDatagram.Ack);
                Assert.AreEqual(0u, sentDatagram.AckField);
                Assert.AreEqual(1u, sentDatagram.Seq);
                CollectionAssert.AreEqual(payload, sentDatagram.Payload);
            }

            {
                var payload2 = new byte[] {0xBE, 0xEF};

                Assert.AreEqual(2u, manager.CurrentMessageId);
                Assert.AreEqual(2u, manager.CurrentSeq);
                manager.SendUnreliable(new IPEndPoint(IPAddress.Any, 0), payload2);

                Assert.AreEqual(2, client.Sent.Count);
                Assert.AreEqual(3u, manager.CurrentMessageId);
                Assert.AreEqual(3u, manager.CurrentSeq);

                var sentDatagram2 = Protocol.Deserialize(client.Sent[1].Item1);

                Assert.AreEqual(2u, sentDatagram2.MessageId);
                Assert.AreEqual(0u, sentDatagram2.Ack);
                Assert.AreEqual(0u, sentDatagram2.AckField);
                Assert.AreEqual(2u, sentDatagram2.Seq);
                CollectionAssert.AreEqual(payload2, sentDatagram2.Payload);
            }

            {
                var emptyPayload = new byte[0];
                client.PushResponse(new Datagram(4, 1, 0, 4, emptyPayload));

                manager.ManualReceive();

                var peekResult = manager.ReceivedQueue.TryPeek(out UdpPair pair);
                Assert.IsTrue(peekResult);

                Assert.AreEqual(4u, pair.Datagram.Seq);
                Assert.AreEqual(1u, pair.Datagram.Ack);
                Assert.AreEqual(0u, pair.Datagram.AckField);
                Assert.AreEqual(4u, pair.Datagram.MessageId);
                CollectionAssert.AreEqual(emptyPayload, pair.Datagram.Payload);
            }

            {
                manager.Update(TimeSpan.FromMilliseconds(16));

                Assert.AreEqual(4u, manager.CurrentAck);
                Assert.AreEqual(0u, manager.CurrentAckField);
            }
        }
    }
}