using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ultimoid.Lib.Tests {
    [TestClass]
    public class UnitTest1 {
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
            sched.RunPeriodicallyLimited(TimeSpan.FromMilliseconds(3),
                TimeSpan.FromMilliseconds(10), 2, () => runCounts++);

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
            sched.RunPeriodically(TimeSpan.FromMilliseconds(5),
                TimeSpan.FromMilliseconds(10), () => unlimitedRunCounts++);

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

        }
    }
}