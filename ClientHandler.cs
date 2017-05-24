﻿using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FAPS
{
    class ClientHandler
    {
        private Middleman monitor;
        private Socket socket;
        private CancellationTokenSource cts;
        private CancellationToken token;
        private ClientSession clientSession;

        private CommandTransceiver cmdTrans = new CommandTransceiver();
        private CommandProcessor cmdProc;


        public ClientHandler(Middleman _monitor, Socket _socket, CancellationTokenSource _cts)
        {
            socket = _socket;
            monitor = _monitor;
            cts = _cts;
            token = cts.Token;
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
            Command cmd = new Command();
            while (!token.IsCancellationRequested && !(clientSession.State.Equals(ClientSession.STATE.stop)))
            {
                try
                { 
                    cmd = cmdTrans.getCmd(socket, clientSession.ID);
                    Console.WriteLine("CH: Received code: " + cmd.nCode);
                    cmdProc.processCommand(cmd);
                }
                catch (SocketException se)
                {
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
            Console.WriteLine("CH: Client handler thread has ended");
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
            ctr.Dispose();
            Console.WriteLine("Client handler sender thread has ended");
        }

    }
}
