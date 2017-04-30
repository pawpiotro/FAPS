using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FAPS
{
    class ProxyServer
    {
        private static Scheduler scheduler;
        private static Monitor monitor;
        // Incoming data from the client.  
        public static string data = null;

        public static void StartListening()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            /*IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            for(int i = 0; i < ipHostInfo.AddressList.Length; i++)
            {
                Console.WriteLine(ipHostInfo.AddressList[i] + "\n");
            }
            IPAddress ipAddress = ipHostInfo.AddressList[0];*/

            IPAddress ipAddress = IPAddress.Parse("192.168.0.16");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and   
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    Socket handler = listener.Accept();
                    data = null;

                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    // Echo the data back to the client.  
                    byte[] msg = Encoding.ASCII.GetBytes(data);

                    handler.Send(msg);
                    if (data.IndexOf("Herro") > -1)
                    {
                        ClientHandler client = new ClientHandler(monitor);
                    }
                    else
                        Console.WriteLine("No threads for ya");
                    Console.WriteLine("Text received : {0}", data);
                    
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }



        public static int Main(String[] args)
        {
            monitor = new Monitor();
            scheduler = new Scheduler(monitor);
            StartListening();
            Console.WriteLine("\nPress ENTER to exit...");
            Console.Read();
            return 0;
        }

    }
}
