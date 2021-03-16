using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    class Session
    {
        int _disconnected = 0;

        Socket _socket;

        public void Start(Socket socket)
        {
            _socket = socket;
            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            recvArgs.SetBuffer(new byte[1024], 0, 1024);

            RegisterReceive(recvArgs);
        }

        public void Send(byte[] sendBuff)
        {
            _socket.Send(sendBuff);
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1) { return; }

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region Network
        private void RegisterReceive(SocketAsyncEventArgs args)
        {
            bool isPending = _socket.ReceiveAsync(args);
            if (!isPending) { OnReceiveCompleted(null, args); }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                // TODO
                try
                {
                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                    Console.WriteLine($"[From Client] {recvData}");
                    RegisterReceive(args);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnReceiveCompleted Failed {e}");
                }
            }
            else
            {
                // TODO Disconnect
            }
        }
        #endregion
    }
}
