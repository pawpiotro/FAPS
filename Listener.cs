using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FAPS
{
    class Listener
    {
        private static Middleman monitor;
        private List<String> connected = new List<String>();

        public Listener(Middleman _monitor)
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
                    try {
                        Console.WriteLine("Waiting for a connection...");
                        handler = listener.Accept();
                        Console.WriteLine("Connected: " + IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)handler.RemoteEndPoint).Port.ToString());

                        // Client send INTRODUCE
                        handler.Receive(cmd.Code);
                        //Console.WriteLine("ncode " + cmd.nCode);
                        if (cmd.nCode.Equals(1))
                        {
                            //Cliend send secret phrase
                            handler.Receive(cmd.Size);
                            cmd.revSize();
                            //Console.WriteLine("nsize " + cmd.nSize);
                            cmd.setDataSize(cmd.Size);
                            handler.Receive(cmd.Data);
                            if (cmd.sData.Equals("zyrafywchodzadoszafy"))
                            {
                                Console.WriteLine("hello");
                                connected.Add(IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)handler.RemoteEndPoint).Port.ToString());
                                ClientHandler client = new ClientHandler(monitor, handler);
                                Thread tmp = new Thread(client.run);
                                tmp.Start();
                            }
                        }
                        else
                            Console.WriteLine("Connection rejected");
                    }
                    catch (SocketException e1)
                    {
                        Console.WriteLine("Unexpected incoming transmission");
                        Console.WriteLine(e1.ToString());
                    }
                    catch (OverflowException e2)
                    {
                        Console.WriteLine(e2.ToString());
                    }
                    catch (OutOfMemoryException e3)
                    {
                        Console.WriteLine(e3.ToString());
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                listener.Shutdown(SocketShutdown.Both);
                listener.Close();
            }
        }
    }
}
