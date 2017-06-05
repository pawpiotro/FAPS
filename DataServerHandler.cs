using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FAPS.Commands;

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
        
        public bool busy = false;
        private Command cmd = null;
        private object cmdLock = new object();
        private int dwnfrag;
        public enum States {download, dwnwait, upload, uplwait, other, idle};
        private States state = States.idle;

        public States State { get { return state; } }

        private CommandTransceiver cmdTrans;

        public DataServerHandler(Middleman _monitor, Scheduler _scheduler, CancellationToken _token, string _address, int _port)
        {
            monitor = _monitor;
            scheduler = _scheduler;
            token = _token;
            address = _address;
            port = _port;
            state = States.idle;
        }

        private void CancelAsync()
        {
            Console.WriteLine("DSH: CANCEL");
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
                //Task.Factory.StartNew(runReceiver, token);
            }
        }

        private bool connect()
        {
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

                cmdTrans = new CommandTransceiver(socket, false);
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
        
        private bool logIn()
        {
            string user = "user2";
            string pass = "pass1";
            Command tcmd = new CommandLogin(user, pass);
            cmdTrans.sendCmd(tcmd);
            tcmd = cmdTrans.getCmd();
            if (tcmd.GetType().Equals(typeof(CommandAccept)))
                return true;
            else
                return false;
        }

        private void runSender()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);

            bool needReconnect = false;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (needReconnect)
                    {
                        if (connect())
                        { 
                        Console.WriteLine("RECONNECTED");
                        needReconnect = false;
                        }
                    }
                    else {
                        Console.WriteLine("DSH: Czekam na SCH");
                        waitForSch();
                        Console.WriteLine("DSH: Obudzony");
                        switch (state)
                        {
                            case States.download:
                                // Here goes download
                                Console.WriteLine("DSH: Dostalem download");
                                startDownload();
                                state = States.idle;
                                break;
                            case States.upload:
                                // Here goes upload
                                Console.WriteLine("DSH: Dostalem Upload");
                                startUpload();
                                state = States.idle;
                                break;
                            case States.other:
                                // Here goes Command send
                                Console.WriteLine("DSH: Dostalem other");
                                startCommand();
                                state = States.idle;
                                break;
                            default:
                                Console.WriteLine("DSH: Co to tu robi");
                                break;
                        }
                    }
                }
                catch (SocketException se)
                {
                    if (state == States.download)
                        scheduler.addFailed((CommandDownload) cmd);
                    if (se.ErrorCode.Equals(10054))
                        needReconnect = true;
                        //break;
                }
                catch (Exception e)
                {
                    if (state == States.download)
                        scheduler.addFailed((CommandDownload)cmd);
                    Console.WriteLine("Connection with Data Server closed");
                    if (e.Message.Equals("Not responding"))
                        needReconnect = true;
                        //break;
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

            Command cmd;
            
            while (!token.IsCancellationRequested)
            {
                try
                {
                    cmd = cmdTrans.getCmd();
                    Console.WriteLine("Received command: " + cmd.GetType());
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

        public bool addDownload(CommandDownload _cmd, int fragment)
        {
            lock (cmdLock)
            {
                Console.WriteLine("DSH: Dodany download");
                dwnfrag = fragment;
                cmd = _cmd;
                state = States.download;
                Monitor.Pulse(cmdLock);
                Console.WriteLine("DSH: Zpulsowano");
                return true;
            }
        }
        public bool addUpload(CommandUpload _cmd)
        {
            lock (cmdLock)
            {
                cmd = _cmd;
                state = States.upload;
                Monitor.Pulse(cmdLock);
                return true;
            }
        }
        public bool addCmd(Command _cmd)
        {
            lock (cmdLock)
            {
                cmd = _cmd;
                state = States.other;
                Monitor.Pulse(cmdLock);
                return true;
            }
        }
        
        private void startDownload()
        {
            Console.WriteLine("Rozpoczeto download");
            CommandDownload dwn = (CommandDownload)cmd;
            cmdTrans.sendCmd(cmd);
            //state = States.dwnwait;
            Command recvd = cmdTrans.getCmd();
            if (recvd.GetType().Equals(typeof(CommandChunk)))
            {
                // Pass the chunk
                Console.WriteLine("Pobieram chunk...");
                monitor.addDownloadChunk((CommandChunk) recvd, dwnfrag);
                scheduler.success(dwnfrag);
                scheduler.wakeSch();
            }
            else
            {
                Console.WriteLine("DSH: Unexpected server response during download: " + recvd.GetType());
                scheduler.addFailed((CommandDownload) cmd);
                scheduler.wakeSch();
            }
        }

        private void startUpload()
        {
            Console.WriteLine("Rozpoczeto upload");
            cmdTrans.sendCmd(cmd);
            CommandUpload upl = (CommandUpload)cmd;
            //int fragments = (int)(upl.Size + scheduler.FragSize - 1 / scheduler.FragSize); // How many chunks to send. Round up.
            long sentSize = 0;
            CommandChunk chunk;
            //state = States.uplwait;
            Command recvd = cmdTrans.getCmd();
            if (recvd.GetType().Equals(typeof(CommandAccept)))
            {
                Console.WriteLine("Upload zaakceptowany");
                scheduler.ConfirmAccept();
                // Upload all the chunks
                int frag = 0;
                //for (int i = 0; i < fragments; i++)
                while (sentSize < upl.Size)
                {
                    chunk = scheduler.takeUplChunk(frag); ;
                    Console.WriteLine("Wysylam chunk o rozmiarze: " + chunk.Data.Length);
                    cmdTrans.sendCmd(chunk);
                    scheduler.uplSucc(frag);
                    scheduler.wakeSch();
                    sentSize += chunk.Data.Length;
                    frag++;
                }
                Console.WriteLine("Wyslano wszystkie chunki.");
                recvd = cmdTrans.getCmd();
                if (!recvd.GetType().Equals(typeof(CommandCommitRdy)))
                    Console.WriteLine("DSH: Unexpected server response after upload: " + recvd.GetType());
                else
                {
                    Console.WriteLine("Czekam na commit...");
                    CommandCommit commit = scheduler.waitForCommit();
                    Console.WriteLine("Wysylam commit...");
                    cmdTrans.sendCmd(commit);
                    recvd = cmdTrans.getCmd();
                    if (!recvd.GetType().Equals(typeof(CommandCommitAck)))
                        Console.WriteLine("DSH: Unexpected server response after upload commit: " + recvd.GetType());
                    else
                        scheduler.ConfirmCommit();
                }
            }
            else
            {
                Console.WriteLine("DSH: Unexpected server response during upload: " + recvd.GetType());
            }
        }

        private void startCommand()
        {
            cmdTrans.sendCmd(cmd);

            if (cmd.GetType().Equals(typeof(CommandDelete)) ||
                cmd.GetType().Equals(typeof(CommandRename)))
            {
                Console.WriteLine("Wysylam delete/rename...");
                Command recvd = cmdTrans.getCmd();
                if (recvd.GetType().Equals(typeof(CommandAccept)))
                {
                    recvd = cmdTrans.getCmd();
                    if (recvd.GetType().Equals(typeof(CommandCommitRdy)))
                    {
                        CommandCommit commit = scheduler.waitForCommit();
                        cmdTrans.sendCmd(commit);
                        Console.WriteLine("Wysylam commit...");
                        recvd = cmdTrans.getCmd();
                        if (!recvd.GetType().Equals(typeof(CommandCommitAck)))
                            Console.WriteLine("DSH: Unexpected server response after cmd commit: " + recvd.GetType());
                        else
                        {
                            Console.WriteLine("Potwierdzono commit.");
                            scheduler.ConfirmCommit();
                        }
                    }
                }
                else
                    Console.WriteLine("DSH: Unexpected server response after cmd: " + recvd.GetType());
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandList)))
            {
                Console.WriteLine("Wysylam list do serwera...");
                Command recvd = cmdTrans.getCmd();
                Console.WriteLine("Dostalem odpowiedz na List");
                if (recvd.GetType().Equals(typeof(CommandChunk)))
                {
                    Console.WriteLine("Wysylam list do klienta...");
                    ((CommandList)cmd).CmdProc.Incoming.Add(recvd, token);
                }
                else
                    Console.WriteLine("DSH: Unexpected server response after cmd: " + recvd.GetType());
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandError)))
            {
                Console.WriteLine("SH: ERROR: " + ((CommandError)cmd).ErrorCode);
                return;
            }
        }

        private void waitForSch()
        {
            lock (cmdLock)
            {
                while (state == States.idle)
                {
                    Monitor.Wait(cmdLock);
                    Console.WriteLine("DSH Zla proba budzenia");
                }
                Console.WriteLine("DSH working");
                Console.WriteLine(state);
                //cmd = null;
            }
        }
    }
}
