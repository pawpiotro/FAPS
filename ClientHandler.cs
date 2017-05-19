using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FAPS
{
    class ClientHandler
    {
        private Middleman monitor;
        private Socket socket;
        private Boolean readyToSend = true;
        private String id;
        private CancellationToken token;

        public ClientHandler(Middleman _monitor, Socket _socket, CancellationToken _token)
        {
            socket = _socket;
            monitor = _monitor;
            token = _token;
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

        private Boolean authenticate(String login, String pass)
        {
            var list = new List<Tuple<String, String>>();
            String[] result;
            String[] separators = { ";" };
            using (StreamReader fs = new StreamReader("pass.txt"))
            {
                String line = null;
                while ((line = fs.ReadLine()) != null)
                {
                    result = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    list.Add(Tuple.Create<String, String>(result[0], result[1]));
                }
            }
            if (list.Contains(Tuple.Create<String, String>(login, pass)))
                return true;
            else
                return false;
        }

        private void connectionCreationConfirmation()
        {
            try
            {
                Command cmd = new Command(Command.cmd.ACCEPT);
                cmd.sendCmd(socket);
                Console.WriteLine(cmd.eCode);
            }catch(SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
        }

        private bool logIn()
        {
            socket.ReceiveTimeout = 3000;
            try
            {
                Command cmd = new Command();
                cmd.getCmd(socket, null);
                
                if (cmd.eCode.Equals(Command.cmd.LOGIN))
                {
                    Console.WriteLine("logging in...");
                    char[] separators = { ':' };
                    String[] tmp = cmd.sData.Split(separators);
                    Console.WriteLine("Login: {0} Pass: {1}", tmp[0], tmp[1]);
                    if (authenticate(tmp[0], tmp[1]))
                    {
                        id = tmp[0];
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Invalid login or password");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Unexpected command received.");
                    return false;
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public void run()
        {
            CancellationTokenRegistration ctr = token.Register(CancelAsync);
            monitor.inc();
            monitor.print();

            connectionCreationConfirmation();
            if (logIn())
            { 
                Console.WriteLine("Success!");
                Console.WriteLine("Waiting for commands...");
                
                // TEMP
                Command cmd = new Command();
                cmd.nCode = 69;
                socket.Send(cmd.Code);
                // 

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        cmd.getCmd(socket, id);
                        Console.WriteLine("Received code: " + cmd.nCode);
                        if (cmd.eCode.Equals(Command.cmd.EXIT))
                        {
                            break;
                        }
                        switch (cmd.nCode)
                        {
                            case 6:
                                monitor.queueDownload(cmd);
                                break;
                            case 7:
                                monitor.queueUpload(cmd);
                                break;
                            default:
                                monitor.queueMisc(cmd);
                                break;
                        }

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
                    finally
                    {
                        cmd = new Command();
                        //System.Threading.Thread.Sleep(1000);
                    }
                }
            }
            else
            {
                Console.WriteLine("Login failed");
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
