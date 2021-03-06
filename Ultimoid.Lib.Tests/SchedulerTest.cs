﻿using System;
using System.Collections.Specialized;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ultimoid.Lib.Tests {
    [TestClass]
    public class SchedulerTest {
        [TestMethod]
        public void BasicSchedulerTest() {
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
    }
}