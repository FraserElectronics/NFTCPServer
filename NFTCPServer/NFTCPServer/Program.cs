using System;
using System.Threading;
using System.Diagnostics;

namespace NFTCPServer
{
    public class Program
    {
        public static void Main()
        {
            _server = new TCPServer() { ListeningOnPort = 54321 };
            _server.Start();
        }

        public static TCPServer _server;
    }
}
