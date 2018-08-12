using System;
using System.Collections.Specialized;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ultimoid.Lib.Tests {
    [TestClass]
    public class UnitTest1 {
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

        [TestMethod]
        public void SchedulerTest() {
            var sched = new Scheduler();

            bool ran = false;
            sched.RunIn(TimeSpan.FromMilliseconds(10), () => ran = true);

            Assert.IsFalse(ran);
            sched.Update(TimeSpan.FromMilliseconds(5));
            Assert.IsFalse(ran);
            sched.Update(TimeSpan.FromMilliseconds(4));
            Assert.IsFalse(ran);
            sched.Update(TimeSpan.FromMilliseconds(1));
            Assert.IsTrue(ran);

            bool ran2 = false;
            sched.RunIn(TimeSpan.FromDays(1), () => ran2 = true);

            Assert.IsFalse(ran2);
            sched.Update(TimeSpan.FromDays(30));
            Assert.IsTrue(ran2);
        }

        [TestMethod]
        public void PeriodicSchedulerTest() {
            var sched = new Scheduler();

            // Limited period
            int runCounts = 0;
            sched.RunPeriodicallyLimited(
                TimeSpan.FromMilliseconds(3),
                TimeSpan.FromMilliseconds(10),
                2,
                () => runCounts++);

            sched.Update(TimeSpan.FromMilliseconds(2));
            Assert.AreEqual(0, runCounts);
            sched.Update(TimeSpan.FromMilliseconds(2));
            Assert.AreEqual(1, runCounts);
            sched.Update(TimeSpan.FromMilliseconds(2));
            Assert.AreEqual(1, runCounts);

            sched.Update(TimeSpan.FromMilliseconds(10));
            Assert.AreEqual(2, runCounts);

            sched.Update(TimeSpan.FromMilliseconds(10));
            Assert.AreEqual(2, runCounts);

            sched.Update(TimeSpan.FromDays(100));
            Assert.AreEqual(2, runCounts);

            // Unlimited period
            int unlimitedRunCounts = 0;
            sched.RunPeriodically(
                TimeSpan.FromMilliseconds(5),
                TimeSpan.FromMilliseconds(10),
                () => unlimitedRunCounts++);

            // TODO: vyjimky u action

            sched.Update(TimeSpan.FromMilliseconds(4));
            Assert.AreEqual(0, unlimitedRunCounts);
            sched.Update(TimeSpan.FromMilliseconds(1));
            Assert.AreEqual(1, unlimitedRunCounts);

            sched.Update(TimeSpan.FromMilliseconds(9));
            Assert.AreEqual(1, unlimitedRunCounts);

            sched.Update(TimeSpan.FromMilliseconds(10));
            Assert.AreEqual(2, unlimitedRunCounts);

            sched.Update(TimeSpan.FromMilliseconds(2));
            Assert.AreEqual(2, unlimitedRunCounts);
            sched.Update(TimeSpan.FromMilliseconds(1));
            Assert.AreEqual(2, unlimitedRunCounts);

            sched.Update(TimeSpan.FromMilliseconds(10));
            Assert.AreEqual(3, unlimitedRunCounts);

            sched.Update(TimeSpan.FromDays(365));
            Assert.AreEqual(4, unlimitedRunCounts);
        }

        [TestMethod]
        public void CancelledTaskTest() {
            var sched = new Scheduler();

            bool ran = false;
            var tcs = sched.RunIn(TimeSpan.FromMilliseconds(10), () => ran = true);

            sched.Update(TimeSpan.FromMilliseconds(5));
            Assert.IsFalse(ran);

            tcs.Cancel();
            sched.Update(TimeSpan.FromMilliseconds(10));
            Assert.IsFalse(ran);
        }

        [TestMethod]
        public void CancelledPeriodicTaskTest() {
            var sched = new Scheduler();

            int count = 0;
            var tcs = sched.RunPeriodically(TimeSpan.FromMilliseconds(5), () => count++);

            sched.Update(TimeSpan.FromMilliseconds(10));
            Assert.AreEqual(1, count);
            sched.Update(TimeSpan.FromMilliseconds(10));
            Assert.AreEqual(2, count);

            tcs.Cancel();

            sched.Update(TimeSpan.FromMilliseconds(10));
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void TestUpdateAckFields() {
            var sched = new Scheduler();
            var network = new NetworkManager(sched);

            network.UpdateSendAckFields(new Datagram(1, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 1uL);
            Assert.AreEqual(network.CurrentAckField, 0b0000_0000_0000_0000u);

            network.UpdateSendAckFields(new Datagram(2, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 2uL);
            Assert.AreEqual(network.CurrentAckField, 0b0000_0000_0000_0001u);

            network.UpdateSendAckFields(new Datagram(3, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 3uL);
            Assert.AreEqual(network.CurrentAckField, 0b0000_0000_0000_0011u);

            network.UpdateSendAckFields(new Datagram(5, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 5uL);
            Assert.AreEqual(network.CurrentAckField, 0b0000_0000_0000_1110u);

            network.UpdateSendAckFields(new Datagram(15, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 15uL);
            Assert.AreEqual(network.CurrentAckField, 0b0011_1010_0000_0000u);

            network.UpdateSendAckFields(new Datagram(31, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 31uL);
            Assert.AreEqual(network.CurrentAckField, 0b0011_1010_0000_0000_1000_0000_0000_0000u);

            network.UpdateSendAckFields(new Datagram(34, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 34uL);
            Assert.AreEqual(network.CurrentAckField, 0b1101_0000_0000_0100_0000_0000_0000_0100u);

            network.UpdateSendAckFields(new Datagram(34, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 0uL);
            Assert.AreEqual(network.CurrentAck, 34uL);
            Assert.AreEqual(network.CurrentAckField, 0b1101_0000_0000_0100_0000_0000_0000_0100u);

            network.SendUnreliable(new IPEndPoint(IPAddress.Broadcast, 0), new byte[] {});

            network.UpdateSendAckFields(new Datagram(128, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 1uL);
            Assert.AreEqual(network.CurrentAck, 128uL);
            Assert.AreEqual(network.CurrentAckField, 0u);

            network.UpdateSendAckFields(new Datagram(131, 0, 0, 0, new byte[] { 0x00 }));

            Assert.AreEqual(network.CurrentSeq, 1uL);
            Assert.AreEqual(network.CurrentAck, 131uL);
            Assert.AreEqual(network.CurrentAckField, 0b0100u);
        }
    }
}