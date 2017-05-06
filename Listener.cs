using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FAPS
{
    class Listener
    {
        private static Monitor monitor;
        private List<String> connected = new List<String>();

        public Listener(Monitor _monitor)
        {
            monitor = _monitor;
        }

        public void printConnected()
        {
            foreach(String s in connected)
            {
                Console.WriteLine(s);
            }
        }

        public void StartListening()
        {
            // Data buffer for incoming data.  
            Command cmd = new Command();

            /*IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            for(int i = 0; i < ipHostInfo.AddressList.Length; i++)
            {
                Console.WriteLine(ipHostInfo.AddressList[i] + "\n");
            }
            IPAddress ipAddress = ipHostInfo.AddressList[0];*/

            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
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
                        if (cmd.sData.Equals("zyrafywchodzadoszafy"))
                        {
                            Console.WriteLine("elo");
                            connected.Add(IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)handler.RemoteEndPoint).Port.ToString());
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
    }
}
