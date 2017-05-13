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
        private Socket listener;
        private Socket handler;

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

        private bool clientIntroduced()
        {
            Command cmd = new Command();
            cmd.getCmd(handler, null);
            Console.WriteLine(cmd.eCode);
            Console.WriteLine(cmd.nSize);
            Console.WriteLine(cmd.sData);
            if (cmd.eCode.Equals(Command.cmd.INTRODUCE))
            {
                if (cmd.sData.Equals("zyrafywchodzadoszafy"))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Incorrect secret phrase.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Connection rejected");
                return false;
            }
        }

        private void handleClient()
        {
            connected.Add(IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)handler.RemoteEndPoint).Port.ToString());
            ClientHandler client = new ClientHandler(monitor, handler);
            Thread tmp = new Thread(client.run);
            tmp.Start();
        }

        public void StartListening()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");//192.168.60.160"); 
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

                listener = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.
                while (true)
                {
                    try {
                        Console.WriteLine("Waiting for a connection...");
                        handler = listener.Accept();
                        Console.WriteLine("Connected: " + IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)handler.RemoteEndPoint).Port.ToString());

                        if (clientIntroduced())
                        {
                            handleClient();
                        }
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
