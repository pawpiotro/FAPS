using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FAPS
{
    class ClientHandler
    {
        private Middleman monitor;
        private Socket socket;
        private CancellationToken token;


        private ClientSession clientSession;

        private CommandTransceiver cmdTrans = new CommandTransceiver();
        private CommandProcessor cmdProc;


        public ClientHandler(Middleman _monitor, Socket _socket, CancellationToken _token)
        {
            socket = _socket;
            monitor = _monitor;
            token = _token;

            clientSession = new ClientSession(monitor);
            cmdProc = new CommandProcessor(clientSession);
        }
        
        public void startThread()
        {
            Task.Factory.StartNew(runReceiver, token);
            Task.Factory.StartNew(runSender, token);
        }

        public void CancelAsync()
        {
            Console.WriteLine("CLIENT HANDLER CANCEL");
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            } catch(SocketException se)
            {
                Console.WriteLine(se.ToString());
            }
        }

        public void runReceiver()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);

            Command cmd = new Command();
            while (!token.IsCancellationRequested && !(clientSession.State.Equals(ClientSession.STATE.stop)))
            {
                try
                { 
                    cmd = cmdTrans.getCmd(socket, clientSession.ID);
                    Console.WriteLine("Received code: " + cmd.nCode);
                    cmdProc.processCommand(cmd);
                }
                catch (SocketException se)
                {
                    //Console.WriteLine(se.ToString());
                }
                catch(Exception e)
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
            Console.WriteLine("Client handler thread has ended");
        }

        public void runSender()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);

            Command cmd = new Command();
            while (!token.IsCancellationRequested && !(clientSession.State.Equals(ClientSession.STATE.stop)))
            {
                try
                {
                    cmd = clientSession.ToSend.Take(token);
                    Console.WriteLine("Sent code: " + cmd.nCode);
                    cmdTrans.sendCmd(socket, cmd);
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se.ToString());
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
            Console.WriteLine("Client handler sender thread has ended");
        }

    }
}
