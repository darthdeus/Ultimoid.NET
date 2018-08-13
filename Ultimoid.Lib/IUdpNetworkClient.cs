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
}