using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace ServerCore
{
    class Listener
    {
        Socket _listenSocket;

        public void Init(IPEndPoint endPoint)
        {
            // Create Socket
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind
            _listenSocket.Bind(endPoint);

            // Listen
            _listenSocket.Listen(10);
        }

        public Socket Accept()
        {
            return _listenSocket.Accept();
        }
    }
}
