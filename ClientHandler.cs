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
        private bool readyToSend = true;
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
        
        public Task startThread()
        {
            return Task.Factory.StartNew(run, token);
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



        private void connectionCreationConfirmation()
        {
            try
            {
                Command cmd = new Command(Command.CMD.ACCEPT);
                cmdTrans.sendCmd(socket, cmd);
            }catch(SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
        }

        public void run()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);
            monitor.inc();
            monitor.print();

            Command cmd = new Command();

            while (!token.IsCancellationRequested)
            {
                cmd = cmdTrans.getCmd(socket, null);
                cmdProc.processCommand(cmd);
                if (clientSession.State.Equals(ClientSession.STATE.idle))
                    break;
                else
                    Console.WriteLine("Login failed");
            }
            
            //LOGGED IN
            Console.WriteLine("Login successful!");
            //SEND CONFIRMATION
            connectionCreationConfirmation();
            //LISTEN FOR COMMANDS
            Console.WriteLine("Waiting for commands...");

            while (!token.IsCancellationRequested && !(clientSession.State.Equals(ClientSession.STATE.stop)))
            {
                try
                { 
                    cmd = cmdTrans.getCmd(socket, clientSession.ID);
                    Console.WriteLine("Received code: " + cmd.nCode);
                    cmdProc.processCommand(cmd);
                }
                catch (SocketException e)
                {
                    //Console.WriteLine("timeout");
                    if (readyToSend)
                    {
                        // Console.WriteLine("wysylam");
                    }
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

    }
}
