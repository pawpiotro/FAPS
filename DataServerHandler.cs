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
        private int serverID;
        
        public bool busy = false;
        private Command cmd = null;
        private object cmdLock = new object();
        private int dwnfrag;
        public enum States {download, dwnwait, upload, uplwait, other, cmdwait, idle, cancel};
        private States state = States.idle;

        public States State { get { return state; } }

        private CommandTransceiver cmdTrans;

        public DataServerHandler(Middleman _monitor, Scheduler _scheduler, CancellationToken _token, string _address, int _port, int _id)
        {
            monitor = _monitor;
            scheduler = _scheduler;
            token = _token;
            address = _address;
            port = _port;
            state = States.idle;
            serverID = _id;
        }

        private void CancelAsync()
        {
            Console.WriteLine("DSH" + serverID + ": CANCEL");
            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
            catch (SocketException se)
            {
                //TEMP
            }
            catch (Exception e)
            {
                //TEMP
            }
        }

        public void startService()
        {
            if (!connect())
                Console.WriteLine("DSH" + serverID + " Login to Data Server Failed");
            else
            {
                Console.WriteLine("DSH" + serverID + " Logged in to data server");
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
                Console.WriteLine("DSH" + serverID + " Socket connected to {0}",
                            socket.RemoteEndPoint.ToString());

                cmdTrans = new CommandTransceiver(socket, false);
                return logIn();
            }
            catch (SocketException se)
            {
                Console.WriteLine("DSH" + serverID + " SocketException : {0}", se.ToString());
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("DSH" + serverID + " Unexpected exception : {0}", e.ToString());
                return false;
            }
        }
        public void disconnect()
        {
            Console.WriteLine("DSH" + serverID + ": DISCONNECT");
            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
            catch (SocketException se)
            {
                //TEMP
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
            //CancellationTokenRegistration ctr = token.Register(CancelAsync);

            bool needReconnect = false;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (needReconnect)
                    {
                        if (connect())
                        { 
                            Console.WriteLine("DSH" + serverID + "RECONNECTED");
                            needReconnect = false;
                        }
                    }
                    else {
                        Console.WriteLine("DSH" +serverID + ": Czekam na SCH");
                        waitForSch();
                        Console.WriteLine("DSH" + serverID + ": Obudzony");
                        switch (state)
                        {
                            case States.download:
                                // Here goes download
                                Console.WriteLine("DSH" + serverID + ": Dostalem download");
                                startDownload();
                                state = States.idle;
                                break;
                            case States.upload:
                                // Here goes upload
                                Console.WriteLine("DSH" + serverID + ": Dostalem Upload");
                                startUpload();
                                state = States.idle;
                                break;
                            case States.other:
                                // Here goes Command send
                                Console.WriteLine("DSH" + serverID + ": Dostalem other");
                                startCommand();
                                state = States.idle;
                                break;
                            case States.cancel:
                                state = States.idle;
                                break;
                            default:
                                Console.WriteLine("DSH" + serverID + ": Co to tu robi");
                                break;
                        }
                    }
                }
                catch (SocketException se)
                {
                    if (state == States.download)
                        scheduler.addFailed((CommandDownload) cmd);
                    if (state == States.upload || state == States.other)
                        scheduler.cancel();
                    if (socket.Connected)
                        if (state == States.uplwait || state == States.cmdwait)
                            rollback();
                        else
                            error(1);
                    //if (se.ErrorCode.Equals(10054))
                    else
                    {
                        Console.WriteLine("Connection with Data Server " + serverID + " closed: SocketException");
                        needReconnect = true;
                    }
                    //break;
                    state = States.idle;
                }
                catch (Exception e)
                {
                    if (state == States.download)
                        scheduler.addFailed((CommandDownload)cmd);
                    if (state == States.upload || state == States.other)
                        scheduler.cancel();
                    if (socket.Connected)
                        if (state == States.uplwait || state == States.cmdwait)
                            rollback();
                        else
                            error(1);
                    //if (e.Message.Equals("Not responding"))
                    else
                    {
                        Console.WriteLine("Connection with Data Server " + serverID + " closed: Exception");
                        needReconnect = true;
                    }
                    //break;
                    state = States.idle;
                }
            }
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            Console.WriteLine("Server handler " + serverID + " sender thread has ended");
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
            CommandDownload dwn = (CommandDownload)cmd;
            Console.WriteLine("DSH" + serverID + " Rozpoczeto download chunka " + dwnfrag + " od " + dwn.Begin +" do " + dwn.End);
            cmdTrans.sendCmd(cmd);
            //state = States.dwnwait;
            Command recvd = cmdTrans.getCmd();
            if (recvd.GetType().Equals(typeof(CommandChunk)))
            {
                // Pass the chunk
                Console.WriteLine("DSH" + serverID + " Pobralem chunk " + dwnfrag + " o rozmiarze " + ((CommandChunk) recvd).Data.Length);
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
                    Console.WriteLine("DSH" + serverID + ": Zasysam nastepnego chunka.");
                    chunk = scheduler.takeUplChunk(frag, serverID); ;
                    Console.WriteLine("DSH" + serverID + ": Wysylam chunk o rozmiarze: " + chunk.Data.Length);
                    cmdTrans.sendCmd(chunk);
                    Console.WriteLine("DSH" + serverID + ": Wyslano chunka.");
                    scheduler.uplSucc(frag);
                    scheduler.wakeSch();
                    Console.WriteLine("DSH" + serverID + ": Obudzilem sch.");
                    sentSize += chunk.Data.Length;
                    frag++;
                }
                Console.WriteLine("DSH" + serverID + ": Wyslano wszystkie chunki.");
                recvd = cmdTrans.getCmd();
                if (!recvd.GetType().Equals(typeof(CommandCommitRdy)))
                    Console.WriteLine("DSH: Unexpected server response after upload: " + recvd.GetType());
                else
                {
                    state = States.uplwait;
                    Console.WriteLine("Czekam na commit...");
                    CommandCommit commit = scheduler.waitForCommit();
                    if (state == States.cancel)
                    {
                        rollback();
                        return;
                    }
                    Console.WriteLine("Wysylam commit...");
                    cmdTrans.sendCmd(commit);
                    recvd = cmdTrans.getCmd();
                    if (!recvd.GetType().Equals(typeof(CommandCommitAck)))
                        Console.WriteLine("DSH: Unexpected server response after upload commit: " + recvd.GetType());
                    else
                    {
                        scheduler.ConfirmCommit();
                        state = States.idle;
                    }
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
                        state = States.cmdwait;
                        CommandCommit commit = scheduler.waitForCommit();
                        if (state == States.cancel)
                        {
                            rollback();
                            return;
                        }
                        cmdTrans.sendCmd(commit);
                        Console.WriteLine("Wysylam commit...");
                        recvd = cmdTrans.getCmd();
                        if (!recvd.GetType().Equals(typeof(CommandCommitAck)))
                            Console.WriteLine("DSH: Unexpected server response after cmd commit: " + recvd.GetType());
                        else
                        {
                            Console.WriteLine("Potwierdzono commit.");
                            scheduler.ConfirmCommit();
                            state = States.idle;
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

        private void rollback()
        {
            CommandRollback rollback =  new CommandRollback();
            cmdTrans.sendCmd(rollback);
        }

        private void error(int code)
        {
            CommandError error = new CommandError(code);
            cmdTrans.sendCmd(error);
        }

        public void cancel()
        {
            state = States.cancel;
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
