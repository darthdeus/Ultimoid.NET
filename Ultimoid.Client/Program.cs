using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Ultimoid {
    public static class Program {
        [STAThread]
        static void Main() {
            var client = new UdpClient();
            byte[] data = Encoding.Default.GetBytes("he");
            client.Send(data, data.Length, "localhost", 8989);
            Console.WriteLine("sent");
            
            using (var game = new Game1()) {
                game.Run();
            }
        }
    }
}