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

        private bool readyToSend = true;
        public bool busy = false;
        private Command cmd = null;
        private static object cmdLock = new object();
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
                //Task.Factory.StartNew(runReceiver, token);
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

            while (!token.IsCancellationRequested)
            {
                try
                {
                    waitForSch();
                    switch (state)
                    {
                        case States.download:
                            // Here goes download
                            startDownload();
                            state = States.idle;
                            break;
                        case States.upload:
                            // Here goes upload
                            startUpload();
                            state = States.idle;
                            break;
                        case States.other:
                            // Here goes Command send
                            startCommand();
                            state = States.idle;
                            break;
                    }
                }
                catch (SocketException se)
                {
                    if (state == States.download)
                        scheduler.addFailed((CommandDownload) cmd);
                    if (se.ErrorCode.Equals(10054))
                        break;
                }
                catch (Exception e)
                {
                    if (state == States.download)
                        scheduler.addFailed((CommandDownload)cmd);
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

        public bool addDownload(CommandDownload _cmd)
        {
            lock (cmdLock)
            {
                cmd = _cmd;
                state = States.download;
                Monitor.Pulse(cmdLock);
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
            CommandDownload dwn = new CommandDownload((CommandDownload)cmd);
            cmdTrans.sendCmd(cmd);
            //state = States.dwnwait;
            Command recvd = cmdTrans.getCmd();
            if (recvd.GetType().Equals(typeof(CommandChunk)))
            {
                // Pass the chunk
                Console.WriteLine("Pobieram chunk...");
                dwn.CmdProc.Incoming.Add(recvd, token);
                scheduler.success(dwn.Begin / scheduler.FragSize);
            }
            else
            {
                Console.WriteLine("DSH: Unexpected server response during download: " + recvd.GetType());
                scheduler.addFailed((CommandDownload) cmd);
            }
        }

        private void startUpload()
        {
            Console.WriteLine("Rozpoczeto upload");
            cmdTrans.sendCmd(cmd);
            CommandUpload upl = new CommandUpload((CommandUpload)cmd);
            int fragments = (int)(upl.Size + scheduler.FragSize - 1 / scheduler.FragSize); // How many chunks to send. Round up.
            CommandChunk chunk;
            //state = States.uplwait;
            Command recvd = cmdTrans.getCmd();
            if (recvd.GetType().Equals(typeof(CommandAccept)))
            {
                upl.CmdProc.Incoming.Add(recvd, token);
                // Upload all the chunks
                Console.WriteLine("Upload zaakceptowany");
                for (int i = 0; i < fragments; i++)
                {
                    Console.WriteLine("Wysylam chunk...");
                    chunk = monitor.UploadChunkQueue.Take(token);
                    cmdTrans.sendCmd(chunk);
                }
                recvd = cmdTrans.getCmd();
                if (!recvd.GetType().Equals(typeof(CommandCommitRdy)))
                    Console.WriteLine("DSH: Unexpected server response after upload: " + recvd.GetType());
                else
                {
                    CommandCommit commit = new CommandCommit();
                    cmdTrans.sendCmd(commit);
                    Console.WriteLine("Wysylam commit...");
                    recvd = cmdTrans.getCmd();
                    if (!recvd.GetType().Equals(typeof(CommandCommitAck)))
                        Console.WriteLine("DSH: Unexpected server response after upload commit: " + recvd.GetType());
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
                    CommandCommit commit = new CommandCommit();
                    cmdTrans.sendCmd(commit);
                    Console.WriteLine("Wysylam commit...");
                    recvd = cmdTrans.getCmd();
                    if (!recvd.GetType().Equals(typeof(CommandCommitAck)))
                        Console.WriteLine("DSH: Unexpected server response after cmd commit: " + recvd.GetType());
                }
                else
                    Console.WriteLine("DSH: Unexpected server response after cmd: " + recvd.GetType());
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandList)))
            {
                Console.WriteLine("Wysylam list...");
                Command recvd = cmdTrans.getCmd();
                if (recvd.GetType().Equals(typeof(CommandChunk)))
                    ; //cmd.CmdProc.Incoming.Add(recvd, token);
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
                }
                Console.WriteLine("DSH working");
                Console.WriteLine(state);
                //cmd = null;
            }
        }
    }
}
