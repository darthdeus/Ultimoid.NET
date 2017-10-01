using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ultimoid.Lib {
    public class NetworkManager {
        private readonly Scheduler _scheduler;

        public NetworkManager(Scheduler scheduler) {
            _scheduler = scheduler;
        }

        public void SendUnreliable(Datagram datagram) {
            DoSendDatagram(datagram);
        }

        public void SendLimitedRetry(Datagram datagram, TimeSpan timeout, TimeSpan retryInterval) {
            DoSendDatagram(datagram);
        }

        private void DoSendDatagram(Datagram datagram) {
        }
    }
}