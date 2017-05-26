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

        private CommandTransceiver cmdTrans;

        public DataServerHandler(Middleman _monitor, Scheduler _scheduler, CancellationToken _token, string _address, int _port)
        {
            monitor = _monitor;
            scheduler = _scheduler;
            token = _token;
            address = _address;
            port = _port;
            cmdTrans = new CommandTransceiver(socket);
        }

        private void CancelAsync()
        {
            Console.WriteLine("SERVER HANDLER CANCEL");
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString());
            }
        }

        public void startService()
        {
            if (!connect())
                Console.WriteLine("Login to Data Server Failed");
            else
            {
                Console.WriteLine("Logged in to data server");
                Task.Factory.StartNew(runSender, token);
                Task.Factory.StartNew(runReceiver, token);
            }
        }

        private bool connect()
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
                return logIn();
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
                return false;
            }
        }

        // ZROBCIE COS Z TYM
        private bool logIn()
        {
            string s = "żołądź:pass1";
            Command tcmd = new Command(Command.CMD.LOGIN, s);
            cmdTrans.sendCmd(tcmd);
            tcmd = cmdTrans.getCmd();
            if (tcmd.eCode.Equals(Command.CMD.ACCEPT))
                return true;
            else
                return false;
        }

        private void runSender()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    waitForSch();
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
                            cmdTrans.sendCmd(cmd);
                            state = State.idle;
                            cmd = null;
                            break;
                    }
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode.Equals(10054))
                        break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connection with Data Server closed");
                    break;
                }
            }
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            Console.WriteLine("Server handler sender thread has ended");
        }

        private void runReceiver()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);

            Command cmd = new Command();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    cmd = cmdTrans.getCmd();
                    Console.WriteLine("Received code: " + cmd.nCode);
                    //cmdProc.processCommand(cmd);
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode.Equals(10054))
                        break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connection with Data Server closed");
                    break;
                }
            }

            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            Console.WriteLine("Server handler receiver thread has ended");
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

        private void waitForSch()
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
            }
        }
    }
}
