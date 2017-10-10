using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace Ultimoid.Lib {
	public struct Datagram {
		public UInt64 Seq;
		public UInt64 Ack;
		public UInt32 AckField;
		public byte[] Payload;
	}

    public struct UdpPair {
        public IPEndPoint Endpoint;
        public Datagram Datagram;

        public UdpPair(IPEndPoint endpoint, Datagram datagram) {
            Endpoint = endpoint;
            Datagram = datagram;
        }
    }

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

		public void ReliableSend(IPEndPoint endpoint, Datagram datagram) {
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

	public static class Protocol {
		public const int MagicHeaderLength = 5;

		private static readonly byte[] MagicBytes = new byte[] { 0xCA, 0xFF, 0xEE, 0xFF, 0xAC };

		const int SeqLength = sizeof(UInt64);
		const int AckLength = sizeof(UInt64);
		const int AckFieldLength = sizeof(UInt32);
		const int HeaderLength = MagicHeaderLength + SeqLength + AckLength + AckFieldLength;
		const int MaxDatagramLength = 256;

		public static Datagram Deserialize(byte[] networkData) {
			if (networkData.Length > MaxDatagramLength) {
				throw new ArgumentException($"Datagram longer than maximum allowed size (actual size: ${networkData.Length}",
					nameof(networkData));
			}

			if (!IsMagicNumberValid(networkData)) {
				throw new ArgumentException($"Attempting to deserialize a datagram in an INVALID format",
					nameof(networkData));
			}

			if (networkData.Length - HeaderLength < 0) {
				throw new ArgumentException(
					$"Network data is shorter than header size (size: ${networkData.Length}, header size: {HeaderLength}",
					nameof(networkData));
			}

			Datagram result;

			byte[] seqBytes = new byte[SeqLength];
			Array.Copy(networkData, MagicHeaderLength, seqBytes, 0, seqBytes.Length);
			seqBytes.ReverseIfLittleEndian();

			result.Seq = BitConverter.ToUInt64(seqBytes, 0);

			byte[] ackBytes = new byte[AckLength];
			Array.Copy(networkData, MagicHeaderLength + SeqLength, ackBytes, 0, ackBytes.Length);
			ackBytes.ReverseIfLittleEndian();

			result.Ack = BitConverter.ToUInt64(ackBytes, 0);

			byte[] ackFieldBytes = new byte[AckFieldLength];
			Array.Copy(networkData, MagicHeaderLength + SeqLength + AckLength, ackFieldBytes, 0, ackFieldBytes.Length);
			ackFieldBytes.ReverseIfLittleEndian();

			result.AckField = BitConverter.ToUInt32(ackFieldBytes, 0);

			byte[] payload = new byte[networkData.Length - HeaderLength];
			Array.Copy(networkData, HeaderLength, payload, 0, payload.Length);

			result.Payload = payload;

			return result;
		}

		public static byte[] Serialize(Datagram datagram) {
			return Serialize(datagram.Payload, datagram.Seq, datagram.Ack, datagram.AckField);
		}

		public static byte[] Serialize(byte[] payload, UInt64 seq, UInt64 ack, UInt32 ackField) {
			var stream = new MemoryStream();

			// Magic header
			stream.Write(MagicBytes, 0, MagicBytes.Length);

			Debug.Assert(stream.Length == MagicHeaderLength);

			byte[] seqBytes = BitConverter.GetBytes(seq);
			seqBytes.ReverseIfLittleEndian();
			stream.Write(seqBytes, 0, seqBytes.Length);

			byte[] ackBytes = BitConverter.GetBytes(ack);
			ackBytes.ReverseIfLittleEndian();
			stream.Write(ackBytes, 0, ackBytes.Length);

			byte[] ackFieldBytes = BitConverter.GetBytes(ackField);
			ackFieldBytes.ReverseIfLittleEndian();
			stream.Write(ackFieldBytes, 0, ackFieldBytes.Length);

			stream.Write(payload, 0, payload.Length);

			return stream.ToArray();
		}

		public static bool IsMagicNumberValid(byte[] data) {
			return data[0] == MagicBytes[0] &&
				   data[1] == MagicBytes[1] &&
				   data[2] == MagicBytes[2] &&
				   data[3] == MagicBytes[3] &&
				   data[4] == MagicBytes[4];
		}

		public static void ReverseIfLittleEndian(this Array arr) {
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(arr);
			}
		}
	}
}