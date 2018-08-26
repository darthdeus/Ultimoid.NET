using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using Ultimoid.Lib;

namespace Ultimoid.Server {
	class Program {
		static void RunServerLoop(int port) {
			var udp = new UdpClient(port);
			udp.DontFragment = true;

			while (true) {
				IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

                Console.WriteLine("Waiting");
				byte[] payload = udp.Receive(ref remote);

			    var rec = string.Join(" ", payload.Select(x => x.ToString()));
                Console.WriteLine($"Received {payload.Length}, IP: {remote}, data: {rec}");
			}
		}

		static void Main(string[] args) {
			RunServerLoop(7999);

            var sched = new Scheduler();

            // TODO: shouldn't use FakeUdpclient
            var network = new NetworkManager(sched, new FakeUdpClient());


		    //var client = new UdpClient(new IPEndPoint(IPAddress.Any, 8989));

		    //while (true) {
		    //    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

		    //    var data = client.Receive(ref sender);
		    //    Console.WriteLine("Received:");
		    //    Console.WriteLine(Encoding.ASCII.GetString(data));
		    //}
		}
	}
}