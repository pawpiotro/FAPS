using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FAPS
{
    class Listener
    {
        private string address;
        private string port;

        private static Middleman monitor;
        private List<string> connected = new List<string>();
        private Socket listener;
        private Socket handler;
        private CancellationToken token;

        public Listener(string _address, string _port, Middleman _monitor, CancellationToken _token)
        {
            address = _address;
            port = _port;
            monitor = _monitor;
            token = _token;
        }

        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        public string Port
        {
            get { return port; }
            set { port = value; }
        }

        public Task startService()
        {
            return Task.Factory.StartNew(StartListening, token);
        }

        public void CancelAsync()
        {
            Console.WriteLine("LISTENER CANCEL");
            
            try
            {
                if (listener.Connected)
                {
                    listener.Shutdown(SocketShutdown.Both);
                }
                listener.Close();
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString());
            }
        }

        public void printConnected()
        {
            foreach(string s in connected)
            {
                Console.WriteLine(s);
            }
        }

        public void StartListening()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);
            try
            {
                IPAddress ipAddress = IPAddress.Parse(address); 
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Int32.Parse(port));

                listener = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("Waiting for a connection...");
                        handler = listener.Accept();
                        Console.WriteLine("Connected: " + IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)handler.RemoteEndPoint).Port.ToString());

                        connected.Add(IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)handler.RemoteEndPoint).Port.ToString());
                        ClientHandler client = new ClientHandler(monitor, handler, token);
                        client.startThread();

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
                if (listener.Connected)
                { 
                    listener.Shutdown(SocketShutdown.Both);
                    listener.Close();
                }
            }
            Console.WriteLine("Listener Thread has ended");
        }
    }
}
