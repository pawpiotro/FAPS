using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FAPS
{
    class DataServerHandler
    {

        private Middleman monitor;
        private Scheduler scheduler;
        private Socket socket;
        private String address;
        private int port;
        private bool readyToSend = true;
        public bool busy = false;
        private Command cmd = null;
        private static object cmdLock = new object();

        public DataServerHandler(Middleman _monitor, Scheduler _scheduler, String _address, int _port)
        {
            monitor = _monitor;
            scheduler = _scheduler;
            address = _address;
            port = _port;
        }

        public bool addCmd(Command _cmd)
        {
            lock (cmdLock)
            {
                cmd = _cmd;
                Monitor.Pulse(cmdLock);
                return true;
            }
        }

        private bool logIn()
        {
            // tudududu
            lock (cmdLock)
            {
                while (cmd == null)
                {
                    Monitor.Wait(cmdLock);
                }
                Console.WriteLine("logIn");
                cmd = null;
                return true;
            }
        }

        public bool send(string file)
        {
            lock (cmdLock)
            {
                readyToSend = true;
                return true;
            }
        }

        public void run()
        {

            monitor.inc();
            monitor.print();

            IPAddress ipAddress = IPAddress.Parse(address);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            //Console.WriteLine(address + ":" + port);

            socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(remoteEP);
                Console.WriteLine("Socket connected to {0}",
                    socket.RemoteEndPoint.ToString());


                socket.ReceiveTimeout = 50;

                if (logIn())
                {
                    Command cmd = new Command();
                    while (true)
                    {
                        try
                        {
                            socket.Receive(cmd.Code);
                            if (cmd.nCode.Equals(255))
                                break;
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine("timeout");
                            if (readyToSend)
                            {
                                Console.WriteLine("wysylam");
                                lock (cmdLock)
                                {
                                    readyToSend = false;
                                }
                            }
                        }
                        finally
                        {
                            cmd = new Command();
                        }
                    }

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                else
                {
                    Console.WriteLine("Connection to server " + address + ":" + port + " failed");
                }
            }

            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }

    }
}
