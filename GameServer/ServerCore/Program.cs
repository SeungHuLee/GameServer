using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Program
    {
        static Listener s_listener = new Listener();

        static void Main(string[] args)
        {
            // DNS 사용
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);

            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);

            try
            {
                s_listener.Init(endPoint);

                while (true)
                {
                    Console.WriteLine("Listening...");

                    // Accept Session Socket
                    Socket clientSocket = s_listener.Accept();

                    // Receive
                    byte[] recvBuf = new byte[1024];
                    int recvBytes = clientSocket.Receive(recvBuf);
                    string recvData = Encoding.UTF8.GetString(recvBuf, 0, recvBytes);

                    Console.WriteLine($"[From Client] {recvData}");

                    // Send

                    byte[] sendBuf = Encoding.UTF8.GetBytes("Welcome to Game Server!");
                    clientSocket.Send(sendBuf);

                    // Close
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
