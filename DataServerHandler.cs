using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FAPS
{
    class DataServerHandler
    {

        private Middleman monitor;
        private Scheduler scheduler;
        private CancellationToken token;

        private Socket socket;
        private string address;
        private int port;

        private bool readyToSend = true;
        public bool busy = false;
        private Command cmd = null;     // current?
        private static object cmdLock = new object();
        private enum State {download, upload, other, idle};
        private State state = State.idle;

        private CommandTransceiver cmdTrans = new CommandTransceiver();

        public DataServerHandler(Middleman _monitor, Scheduler _scheduler, CancellationToken _token, string _address, int _port)
        {
            monitor = _monitor;
            scheduler = _scheduler;
            token = _token;
            address = _address;
            port = _port;
        }


        public Task startService()
        {
            return Task.Factory.StartNew(run, token);
        }

        // ZROBCIE COS Z TYM
        private bool logIn()
        {
            string s = "user2:pass1";
            Command tcmd = new Command(Command.CMD.LOGIN, s);
            cmdTrans.sendCmd(socket, tcmd);
            tcmd = cmdTrans.getCmd(socket, null);
            if (tcmd.eCode.Equals(Command.CMD.ACCEPT))
                return true;
            else
                return false;
        }

        public bool addDownload(string file, int frag)
        {
            lock (cmdLock)
            {
                state = State.download;
                Monitor.Pulse(cmdLock);
                return true;
            }
        }
        public bool addUpload(string file)
        {
            lock (cmdLock)
            {
                state = State.upload;
                Monitor.Pulse(cmdLock);
                return true;
            }
        }
        public bool addCmd(Command _cmd)
        {
            lock (cmdLock)
            {
                cmd = _cmd;
                state = State.other;
                Monitor.Pulse(cmdLock);
                return true;
            }
        }

        public bool send(string file)   // BETA MAYBE USELESS
        {
            lock (cmdLock)
            {
                readyToSend = true;
                return true;
            }
        }

        private bool waitForSch()
        {
            lock (cmdLock)
            {
                while (state == State.idle)
                {
                    Monitor.Wait(cmdLock);
                }
                Console.WriteLine("DSH working");
                Console.WriteLine(state);
                //cmd = null;
                return true;
            }
        }

        public void run()
        {

            Console.WriteLine("DATASERVER: " + address + ":" + port);
            
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

                //send LOGIN
                if (logIn())
                {
                    Console.WriteLine("Logged in to data server");
                    while (waitForSch())
                    {
                        switch (state)
                        {
                            case State.download:
                                // Here goes download
                                Console.WriteLine("Download");
                                state = State.idle;
                                break;
                            case State.upload:
                                // Here goes upload
                                Console.WriteLine("Upload");
                                state = State.idle;
                                break;
                            case State.other:
                                // Here goes command send
                                Console.WriteLine("Wysyłam");
                                cmdTrans.sendCmd(socket, cmd);
                                state = State.idle;
                                cmd = null;
                                break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Login failed");
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
