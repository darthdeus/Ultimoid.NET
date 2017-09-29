﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Ultimoid.Server {
    class Program {
        static void Main(string[] args) {
            var client = new UdpClient(new IPEndPoint(IPAddress.Any, 8989));

            while (true) {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                var data = client.Receive(ref sender);
                Console.WriteLine("Received:");
                Console.WriteLine(Encoding.ASCII.GetString(data));
            }
        }
    }
}