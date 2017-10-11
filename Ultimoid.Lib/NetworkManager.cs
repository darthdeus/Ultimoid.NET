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

        private ulong _currentSeq;

        private ulong _currentAck;
        private uint _currentAckField;

        public NetworkManager(Scheduler scheduler) {
            _scheduler = scheduler;

            // TODO: listen port
            _udp = new UdpClient(ListenPort);

            _currentSeq = 0;
        }

        public CancellationTokenSource StartWorkerThreads() {
            var cts = new CancellationTokenSource();

            var resetEvent = new ManualResetEvent(false);

            // Sender thread
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

            // Receiver thread
            new Thread(() => {
                CancellationToken token = cts.Token;


                while (!token.IsCancellationRequested) {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] payload = _udp.Receive(ref remote);

                    Datagram datagram = Protocol.Deserialize(payload);

                    UpdateAckFields(datagram);

                    _receivedQueue.Enqueue(new UdpPair(remote, datagram));
                }
            }).Start();

            return cts;
        }

        private void UpdateAckFields(Datagram receivedDatagram) {
            // TODO: handle ack/seq re-calculation
            if (receivedDatagram.Seq > _currentAck) {
                
            }
        }

        public void SendUnreliable(IPEndPoint endpoint, byte[] data) {
            DoSend(endpoint, data);
        }

        public void SendLimitedRetry(IPEndPoint endpoint, byte[] data, TimeSpan timeout, TimeSpan retryInterval) {
            DoSend(endpoint, data);

            // TODO: doplnit
        }


        public void SendReliable(IPEndPoint endpoint, byte[] data) {
            // TODO: vymyslet generovani seq
            DoSend(endpoint, data);

            // TODO: compute based on initial handshake?
            TimeSpan retryPeriod = TimeSpan.FromMilliseconds(200);
            int retryCount = 3;

            var tcs = _scheduler.RunPeriodicallyLimited(retryPeriod, retryCount, () => {
                // TODO: compute seq
                DoSend(endpoint, data);
            });
        }


        private void DoSend(IPEndPoint endpoint, byte[] data) {
            // TODO: samotne odeslani datagramu

            var datagram = new Datagram(
                _currentSeq++,
                _currentAck,
                _currentAckField,
                data);

            // TODO: update acks

            _sendQueue.Enqueue(new UdpPair(endpoint, datagram));
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