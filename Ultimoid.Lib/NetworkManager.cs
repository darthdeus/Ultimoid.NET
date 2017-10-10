using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ultimoid.Lib {
    public class NetworkManager {
        const int ListenPort = 9987;

        private Scheduler _scheduler;
        private Dictionary<ulong, CancellationToken> _pending = new Dictionary<ulong, CancellationToken>();

        private ConcurrentQueue<UdpPair> _receivedQueue = new ConcurrentQueue<UdpPair>();
        private ConcurrentQueue<UdpPair> _sendQueue = new ConcurrentQueue<UdpPair>();

        private UdpClient _udp;

        public NetworkManager(Scheduler scheduler) {
            _scheduler = scheduler;

            // TODO: listen port
            _udp = new UdpClient(ListenPort);
        }

        public CancellationTokenSource StartWorkerThread() {
            var cts = new CancellationTokenSource();

            var resetEvent = new ManualResetEvent(false);

            new Thread(() => {
                CancellationToken token = cts.Token;

                var udpSenderClient = new UdpClient();

                while (!token.IsCancellationRequested) {
                    UdpPair sendRequest;

                    if (_sendQueue.TryDequeue(out sendRequest)) {
                        udpSenderClient.Connect(sendRequest.Endpoint);

                        byte[] data = Protocol.Serialize(sendRequest.Datagram);
                        udpSenderClient.Send(data, data.Length);

                        if (_sendQueue.IsEmpty) {
                            resetEvent.Reset();
                        }
                    } else {
                        Thread.Sleep(TimeSpan.FromMilliseconds(5));
                        //resetEvent.
                        //resetEvent.WaitOne(TimeSpan.FromMilliseconds(10));
                    }
                }
            }).Start();

            new Thread(() => {
                CancellationToken token = cts.Token;


                while (!token.IsCancellationRequested) {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] payload = _udp.Receive(ref remote);

                    Datagram datagram = Protocol.Deserialize(payload);

                    _receivedQueue.Enqueue(new UdpPair(remote, datagram));
                }
            }).Start();

            return cts;
        }

        public void SendUnreliable(IPEndPoint endpoint, Datagram datagram) {
            DoSend(endpoint, datagram);
        }

        public void SendLimitedRetry(Datagram datagram, TimeSpan timeout, TimeSpan retryInterval) {
            DoSendDatagram(datagram);
        }


        public void SendReliable(IPEndPoint endpoint, Datagram datagram)
        {
            // TODO: vymyslet generovani seq
            DoSend(endpoint, datagram);

            // TODO: compute based on initial handshake?
            TimeSpan retryPeriod = TimeSpan.FromMilliseconds(200);
            int retryCount = 3;

            var tcs = _scheduler.RunPeriodicallyLimited(retryPeriod, retryCount, () => {
                // TODO: compute seq
                DoSend(endpoint, datagram);
            });
        }


        private void DoSend(IPEndPoint endpoint, Datagram datagram) {
            // TODO: samotne odeslani datagramu
        }

        public void Update(TimeSpan deltaTime) {
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);


            byte[] payload = _udp.Receive(ref sender);
        }

        public bool TryReceive(out UdpPair incoming) {
            return _receivedQueue.TryDequeue(out incoming);
        }
    }
}