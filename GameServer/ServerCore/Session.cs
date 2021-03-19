using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    public abstract class Session
    {
        int _disconnected = 0;
        object _lock = new object();

        ReceiveBuffer _recvBuffer = new ReceiveBuffer(1024);

        Socket _socket;
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        Queue<byte[]> _sendQueue = new Queue<byte[]>();

        public void Start(Socket socket)
        {
            _socket = socket;
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            _recvArgs.SetBuffer(new byte[1024], 0, 1024);

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterReceive();
        }

        public void Send(byte[] sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0) { RegisterSend(); }
            }
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1) { return; }

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region Network

        private void RegisterSend()
        {
            while (_sendQueue.Count > 0)
            {
                byte[] buff = _sendQueue.Dequeue();
                _pendingList.Add(new ArraySegment<byte>(buff, 0, buff.Length));
            }

            _sendArgs.BufferList = _pendingList;

            bool isPending = _socket.SendAsync(_sendArgs);
            if (!isPending) { OnSendCompleted(null, _sendArgs); }
        }

        private void RegisterReceive()
        {
            _recvBuffer.Clean();

            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool isPending = _socket.ReceiveAsync(_recvArgs);
            if (!isPending) { OnReceiveCompleted(null, _recvArgs); }
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)
                        {
                            RegisterSend();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // Move Buffer Write Position 
                    if (!_recvBuffer.OnWrite(args.BytesTransferred))
                    {
                        Disconnect();
                        return;
                    }

                    // Check how much data was sent to content-end
                    int processLength = OnReceive(_recvBuffer.ReadSegment);
                    if (processLength < 0 || _recvBuffer.DataSize < processLength)
                    {
                        Disconnect();
                        return;
                    }

                    // Move Buffer Read Position
                    if (!_recvBuffer.OnRead(processLength))
                    {
                        Disconnect();
                        return;
                    }

                    RegisterReceive();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnReceiveCompleted Failed {e}");
                }
            }
            else
            {
                Disconnect();
            }
        }
        #endregion
        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnReceive(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);
    }
}