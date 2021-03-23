using System;
using System.Net;
using ServerCore;

namespace GameServer
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

            s_listener.Init(endPoint, () => { return new ClientSession(); });

            Console.WriteLine("Listening...");

            while (true)
            {

            }
        }
    }
}
