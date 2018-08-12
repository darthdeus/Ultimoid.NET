using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ultimoid.Lib {
    public class NetworkManager {
        const int ListenPort = 9987;

        private readonly Scheduler _scheduler;
        private Dictionary<ulong, CancellationTokenSource> _pending = new Dictionary<ulong, CancellationTokenSource>();

        private readonly ConcurrentQueue<UdpPair> _receivedQueue = new ConcurrentQueue<UdpPair>();
        private readonly ConcurrentQueue<UdpPair> _sendQueue = new ConcurrentQueue<UdpPair>();

        private UdpClient _udp;

        private ulong _currentSeq;
        public ulong CurrentSeq => _currentSeq;

        private ulong _currentAck;
        public ulong CurrentAck => _currentAck;
        private uint _currentAckField;
        public uint CurrentAckField => _currentAckField;

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
                    // TODO: synchronne?
                    UdpPair sendRequest;
                    if (_sendQueue.TryDequeue(out sendRequest)) {
                        udpSenderClient.Connect(sendRequest.Endpoint);

                        byte[] data = Protocol.Serialize(sendRequest.Datagram);
                        // TODO: send async?
                        udpSenderClient.Send(data, data.Length);

                        //if (_sendQueue.IsEmpty) {
                        //    resetEvent.Reset();
                        //}
                    } else {
                        Thread.Sleep(TimeSpan.FromMilliseconds(3));
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

                    ReceiveDatagram(remote, datagram);
                }
            }).Start();

            return cts;
        }

        public void ReceiveDatagram(IPEndPoint remote, Datagram datagram) {
            UpdateAckFields(datagram);

            _receivedQueue.Enqueue(new UdpPair(remote, datagram));
        }

        public void UpdateAckFields(Datagram receivedDatagram) {
            // TODO: handle ack/seq re-calculation
            // TODO: neni threadsafe _currentAck + _currentAckField
            if (receivedDatagram.Seq > _currentAck) {
                int diff = (int) (receivedDatagram.Seq - _currentAck);

                // current: 10, received: 13, diff = 3

                // current = 10
                //      v - potvrzuje 9. packet
                // 000101
                
                // potom, kdyz current = 13
                //      v - potvrzuje 12. packet
                // 101000

                _currentAckField <<= 1;
                if (_currentAck != 0) {
                    _currentAckField |= 1u;
                }

                for (int i = 0; i < diff - 1; i++) {
                    _currentAckField <<= 1;
                }

                _currentAck = receivedDatagram.Seq;
            } else if (receivedDatagram.Seq == _currentAck) {
                // TODO: Duplicate datagram? Raise error or log?
                return;
            } else {
                int diff = (int)(_currentAck - receivedDatagram.Seq);

                // curr = 5,  received = 4, diff = 1
                // curr = 5,  received = 3, diff = 2
                // curr = 10, received = 3, diff = 7

                // ackfield: 00010010101100000, ack: 44, rec_seq: 40, diff=4
                // ackfield: 00010010101100000, ack: 44, rec_seq: 43, diff=1
                Debug.Assert(diff >= 1);

                if (diff <= 32) {
                    _currentAckField |= 1u << (diff - 1);
                } else {
                    // TODO: log old datagram
                }
            }
        }

        public void SendUnreliable(IPEndPoint endpoint, byte[] data) {
            DoSend(endpoint, data);
        }

        public void SendLimitedRetry(IPEndPoint endpoint, byte[] data, TimeSpan timeout, TimeSpan retryInterval) {
            DoSend(endpoint, data);

            int retryCount = (int)Math.Ceiling(timeout.TotalMilliseconds / retryInterval.TotalMilliseconds);
            // TODO: doplnit
        }

        public void SendReliable(IPEndPoint endpoint, byte[] data) {
            ulong seq = DoSend(endpoint, data);

            // TODO: compute based on initial handshake?
            TimeSpan retryPeriod = TimeSpan.FromMilliseconds(200);
            int retryCount = 3;

            var tcs = _scheduler.RunPeriodicallyLimited(retryPeriod, retryCount, () => {
                // TODO: compute seq
                DoSend(endpoint, data);
            });

            // TODO: race condition, pokud mi prijde odpoved driv, nez ulozim task
            _pending[seq] = tcs;
        }


        private ulong DoSend(IPEndPoint endpoint, byte[] data) {
            // TODO: samotne odeslani datagramu
            ulong seq = _currentSeq++;

            var datagram = new Datagram(
                seq,
                _currentAck,
                _currentAckField,
                data);

            // TODO: update acks

            _sendQueue.Enqueue(new UdpPair(endpoint, datagram));

            return seq;
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