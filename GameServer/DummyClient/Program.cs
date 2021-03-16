using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DummyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // DNS 사용
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);

            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);

            while (true)
            {
                // Socket, TCP
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    // Connect
                    socket.Connect(endPoint);
                    Console.WriteLine($"Connected To {socket.RemoteEndPoint.ToString()}");

                    // Send
                    for (int i = 0; i < 5; i++)
                    {
                        byte[] sendBuf = Encoding.UTF8.GetBytes($"Hello Server from Client! {i}\n");
                        int sendBytes = socket.Send(sendBuf);
                    }

                    // Receive
                    byte[] recvBuf = new byte[1024];
                    int recvBytes = socket.Receive(recvBuf);

                    string recvData = Encoding.UTF8.GetString(recvBuf, 0, recvBytes);
                    Console.WriteLine($"[From Server] {recvData}");

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(100);
            }
        }
    }
}
