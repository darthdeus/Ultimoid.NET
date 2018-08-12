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

        private readonly Dictionary<ulong, Tuple<CancellationTokenSource, TaskCompletionSource<uint>>> _pendingAck
            = new Dictionary<ulong, Tuple<CancellationTokenSource, TaskCompletionSource<uint>>>();

        private readonly ConcurrentQueue<UdpPair> _receivedQueue = new ConcurrentQueue<UdpPair>();
        private readonly ConcurrentQueue<UdpPair> _sendQueue = new ConcurrentQueue<UdpPair>();

        private readonly UdpClient _udp;

        public ulong CurrentSeq { get; private set; } = 0;
        public ulong CurrentAck { get; private set; } = 0;
        public uint CurrentAckField { get; private set; } = 0;
        public uint CurrentMessageId { get; private set; } = 0;

        private readonly Dictionary<ulong, uint> _localSeqToMessageId = new Dictionary<ulong, uint>();
        private readonly TimeSpan TimeoutTimeSpan = TimeSpan.FromSeconds(5);

        public NetworkManager(Scheduler scheduler) {
            _scheduler = scheduler;
            _udp = new UdpClient(ListenPort);
        }

        public CancellationTokenSource StartWorkerThreads() {
            var cts = new CancellationTokenSource();

            // Receiver thread
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

        public void UpdateSendAckFields(Datagram receivedDatagram) {
            // TODO: handle ack/seq re-calculation
            // TODO: neni threadsafe _currentAck + _currentAckField
            if (receivedDatagram.Seq > CurrentAck) {
                int diff = (int) (receivedDatagram.Seq - CurrentAck);

                // current: 10, received: 13, diff = 3

                // current = 10
                //      v - potvrzuje 9. packet
                // 000101

                // potom, kdyz current = 13
                //      v - potvrzuje 12. packet
                // 101000

                CurrentAckField <<= 1;
                if (CurrentAck != 0) {
                    CurrentAckField |= 1u;
                }

                for (int i = 0; i < diff - 1; i++) {
                    CurrentAckField <<= 1;
                }

                CurrentAck = receivedDatagram.Seq;
            } else if (receivedDatagram.Seq == CurrentAck) {
                // TODO: Duplicate datagram? Raise error or log?
                return;
            } else {
                int diff = (int) (CurrentAck - receivedDatagram.Seq);

                // curr = 5,  received = 4, diff = 1
                // curr = 5,  received = 3, diff = 2
                // curr = 10, received = 3, diff = 7

                // ackfield: 00010010101100000, ack: 44, rec_seq: 40, diff=4
                // ackfield: 00010010101100000, ack: 44, rec_seq: 43, diff=1
                Debug.Assert(diff >= 1);

                if (diff <= 32) {
                    CurrentAckField |= 1u << (diff - 1);
                } else {
                    // TODO: log old datagram
                }
            }
        }

        public void SendUnreliable(IPEndPoint endpoint, byte[] data) {
            DoSend(endpoint, CurrentMessageId++, data);
        }

        public Task<uint> SendLimitedRetry(IPEndPoint endpoint, uint messageId, byte[] data, TimeSpan timeout,
            TimeSpan retryInterval) {
            throw new NotImplementedException();

            //uint msgid = CurrentMessageId++;
            //DoSend(endpoint, msgid, data);

            //int retryCount = (int) Math.Ceiling(timeout.TotalMilliseconds / retryInterval.TotalMilliseconds);

            //var cts = _scheduler.RunPeriodicallyLimited(retryInterval, retryCount,
            //    () => { DoSend(endpoint, messageId, data); });

            //var completionTcs = new TaskCompletionSource<uint>();
            //_pendingAck[msgid] = Tuple.Create(cts, completionTcs);
            //return completionTcs.Task;
        }

        public Task<uint> SendReliable(IPEndPoint endpoint, byte[] data) {
            uint msgid = CurrentMessageId++;

            // TODO: compute based on initial handshake?
            TimeSpan retryPeriod = TimeSpan.FromMilliseconds(200);

            // TODO: add a limit?
            // TODO: detect disconnect
            var cts = _scheduler.RunPeriodically(retryPeriod, () => {
                ulong seq = DoSend(endpoint, msgid, data);
                _localSeqToMessageId[seq] = msgid;

                _scheduler.RunIn(TimeoutTimeSpan, () => _localSeqToMessageId.Remove(seq));
            });

            var completionTcs = new TaskCompletionSource<uint>();
            _pendingAck[msgid] = Tuple.Create(cts, completionTcs);

            return completionTcs.Task;
        }

        private ulong DoSend(IPEndPoint endpoint, uint messageId, byte[] payload) {
            ulong seq = CurrentSeq++;

            var datagram = new Datagram(seq, CurrentAck, CurrentAckField, messageId, payload);

            var udpSenderClient = new UdpClient(endpoint);
            byte[] data = Protocol.Serialize(datagram);
            // TODO: send async?
            udpSenderClient.Send(data, data.Length);

            return seq;
        }

        public void Update(TimeSpan deltaTime) {
            while (true) {
                if (_receivedQueue.TryDequeue(out UdpPair incomingPair)) {
                    var dgram = incomingPair.Datagram;
                    UpdateSendAckFields(dgram);

                    // TODO: update acks
                    foreach (ulong ackedSeq in dgram.ParseAckedSeqs()) {
                        uint messageId = _localSeqToMessageId[ackedSeq];

                        if (_pendingAck.TryGetValue(messageId, out var value)) {
                            value.Item1.Cancel();
                            value.Item2.SetResult(messageId);
                            _pendingAck.Remove(messageId);
                        }
                    }
                } else {
                    break;
                }
            }
        }
    }
}