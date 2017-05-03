using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace FAPS
{
    class ProxyServer
    {
        private static Scheduler scheduler;
        private static Monitor monitor;

        public static void StartListening()
        {
            // Data buffer for incoming data.  
            Command cmd = new Command();

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
            Socket handler = null;

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                
                // Start listening for connections.
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    handler = listener.Accept();

                    // Client send INTRODUCE
                    handler.Receive(cmd.Code);
                    Console.WriteLine("ncode " + cmd.nCode);
                    if (cmd.nCode.Equals(1))
                    {
                        //Cliend send secret phrase
                        handler.Receive(cmd.Size);
                        Console.WriteLine("nsize " + cmd.nSize);
                        cmd.setDataSize(cmd.Size);
                        handler.Receive(cmd.Data);
                        if (cmd.sData.Equals("zyrafywchodzadoszafy")){
                            Console.WriteLine("elo");
                            ClientHandler client = new ClientHandler(monitor, handler);
                            Thread tmp = new Thread(client.run);
                            tmp.Start();
                        }
                    }
                    else
                        Console.WriteLine("Connection rejected");
                    
                }

            }
            catch (Exception e)
            {

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                Console.WriteLine(e.ToString());
            }
            
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
