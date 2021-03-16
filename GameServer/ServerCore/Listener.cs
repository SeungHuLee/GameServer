using System;
using System.Net.Sockets;
using System.Net;

namespace ServerCore
{
    class Listener
    {
        Socket _listenSocket;
        Action<Socket> _onAcceptHandler;

        public void Init(IPEndPoint endPoint, Action<Socket> onAcceptHandler)
        {
            // Create Socket
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _onAcceptHandler += onAcceptHandler;

            // Bind
            _listenSocket.Bind(endPoint);

            // Listen
            _listenSocket.Listen(10);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args);
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            bool isPending = _listenSocket.AcceptAsync(args);

            if (!isPending) { OnAcceptCompleted(null, args); }
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                _onAcceptHandler.Invoke(args.AcceptSocket);
            }
            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }
            RegisterAccept(args);
        }
    }
}
