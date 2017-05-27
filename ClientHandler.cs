using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FAPS
{
    class ClientHandler
    {
        private Socket socket;
        private CancellationTokenSource cts;
        private CancellationToken token;

        private NetworkFrameTransceiver cmdTrans;
        private NetworkFrameProcessor cmdProc;


        public ClientHandler(Middleman _monitor, Socket _socket, CancellationTokenSource _cts)
        {
            socket = _socket;
            cts = _cts;
            token = cts.Token;
            cmdTrans = new NetworkFrameTransceiver(socket);
            cmdProc = new NetworkFrameProcessor(_monitor, cts);

            startThread();
        }
        
        private void startThread()
        {
            Task.Factory.StartNew(runReceiver, token);
            Task.Factory.StartNew(runSender, token);
        }

        public void CancelAsync()
        {
            Console.WriteLine("CH: CLIENT HANDLER CANCEL");
            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            } catch(SocketException se)
            {
                Console.WriteLine(se.ToString());
            } //catch(ObjectDisposedException ode)
        }

        public void runReceiver()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);
            NetworkFrame cmd = new NetworkFrame();
            while (!token.IsCancellationRequested)
            {
                try
                { 
                    cmd = cmdTrans.getCmd();
                    Console.WriteLine("CH: Received code: " + cmd.nCode);
                    cmdProc.Incoming.Add(cmd);
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode.Equals(10054))
                        break;
                    //Console.WriteLine(se.ToString());
                }
                catch(Exception e)
                {
                    Console.WriteLine("CH: Connection with client closed");
                    break;
                }
            }

            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            ctr.Dispose();
            Console.WriteLine("CH: Client handler receiver thread has ended");
        }

        public void runSender()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);

            NetworkFrame cmd = new NetworkFrame();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    cmd = cmdProc.ToSend.Take(token);
                    Console.WriteLine("Sent code: " + cmd.nCode);
                    cmdTrans.sendCmd(cmd);
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode.Equals(10054))
                        break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connection with client closed");
                    break;
                }
            }

            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            ctr.Dispose();
            Console.WriteLine("Client handler sender thread has ended");
        }

    }
}
