﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Ultimoid.Lib {
    public interface IUdpNetworkClient {
        byte[] Receive(ref IPEndPoint remote);
        int Send(byte[] data, int length, IPEndPoint remoteEndpoint);
    }

    public class UdpNetworkClient : IUdpNetworkClient {
        private readonly UdpClient _client;

        public UdpNetworkClient(UdpClient client) {
            _client = client;
        }

        public byte[] Receive(ref IPEndPoint remote) {
            return _client.Receive(ref remote);
        }

        public int Send(byte[] data, int length, IPEndPoint remoteEndpoint) {
            return _client.Send(data, length, remoteEndpoint);
        }
    }

    public class FakeUdpClient : IUdpNetworkClient {
        public readonly List<byte[]> Responses = new List<byte[]>();
        public readonly List<Tuple<byte[], IPEndPoint>> Sent = new List<Tuple<byte[], IPEndPoint>>();

        public void PushResponse(Datagram datagram) {
            Responses.Add(Protocol.Serialize(datagram));
        }

        public byte[] Receive(ref IPEndPoint remote) {
            if (Responses.Count == 0) {
                throw new InvalidOperationException();
            }
            var response = Responses[0];
            Responses.RemoveAt(0);
            return response;
        }

        public int Send(byte[] data, int length, IPEndPoint remoteEndpoint) {
            if (data.Length != length) {
                throw new ArgumentException();
            }
            Sent.Add(Tuple.Create(data, remoteEndpoint));
            return length;
        }
    }
}