using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FAPS.Commands;

namespace FAPS
{
    class ClientHandler
    {
        private Socket socket;
        private CancellationTokenSource cts;
        private CancellationToken token;

        private CommandTransceiver cmdTrans;
        private CommandProcessor cmdProc;


        public ClientHandler(Middleman _monitor, Socket _socket, CancellationTokenSource _cts)
        {
            socket = _socket;
            cts = _cts;
            token = cts.Token;
            cmdTrans = new CommandTransceiver(socket, true);
            cmdProc = new CommandProcessor(_monitor, cts);

            startThread();
        }
        
        private void startThread()
        {
            Task.Factory.StartNew(runReceiver, token);
            Task.Factory.StartNew(runSender, token);
        }

        public void CancelAsync()
        {
            Console.WriteLine("CH " + cmdProc.ID + ": CANCEL");
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
                Console.WriteLine(se.ToString());
            } //catch(ObjectDisposedException ode)
        }

        public void runReceiver()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);
            Command cmd;
            while (!token.IsCancellationRequested)
            {
                try
                { 
                    cmd = cmdTrans.getCmd();
                    if (!cmd.Equals(null))
                    {
                        if(cmdProc.ID.Equals(""))
                            Console.WriteLine("CH *new*: Received command: " + cmd.GetType());
                        else
                            Console.WriteLine("CH " + cmdProc.ID + ": Received command: " + cmd.GetType());
                        cmdProc.Incoming.Add(cmd, token);
                    }
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode.Equals(10054))
                        break;
                }
                catch(Exception e)
                {
                    Console.WriteLine("CH " + cmdProc.ID + ": Receiver: " + e.Message);
                    if(e.Message.Equals("Not responding"))
                        break;
                }
            }

            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            if(!token.IsCancellationRequested)
                cts.Cancel();
            ctr.Dispose();
            Console.WriteLine("CH " + cmdProc.ID + ": Receiver: Client handler thread has ended");
        }

        public void runSender()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);

            Command cmd;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    cmd = cmdProc.ToSend.Take(token);
                    Console.WriteLine("CH " + cmdProc.ID + ": Sent command: " + cmd.GetType());
                    cmdTrans.sendCmd(cmd);
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode.Equals(10054))
                        break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("CH " + cmdProc.ID + ": Receiver: " + e.Message);
                    if (e.Message.Equals("Not responding"))
                        break;
                }
            }

            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            if (!token.IsCancellationRequested)
                cts.Cancel();
            ctr.Dispose();
            Console.WriteLine("CH " + cmdProc.ID + ": Sender: Client handler thread has ended");
        }

    }
}
