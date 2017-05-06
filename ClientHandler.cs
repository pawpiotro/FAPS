using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace FAPS
{
    class ClientHandler
    {
        private Monitor monitor;
        private Socket socket;
        private Boolean readyToSend = true;
        private String id;
        

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

        public ClientHandler(Monitor _monitor, Socket _socket)
        {
            socket = _socket;
            monitor = _monitor;
        }

        private bool logIn()
        {
            Command cmd = new Command();
            // send ACCEPT to client
            cmd.nCode = 8;
            socket.Send(cmd.Code);
            // receive command. expecting LOGIN
            socket.Receive(cmd.Code);
            if (cmd.nCode.Equals(2))
            {
                Console.WriteLine("logging in...");
                // receive size of login and password
                socket.Receive(cmd.Size);
                Console.WriteLine(cmd.nSize);
                cmd.setDataSize(cmd.Size);
                socket.Receive(cmd.Data);
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
            } else
            {
                Console.WriteLine("Unexpected command received.");
                return false;
            }
        }

        public void run()
        {

            monitor.inc();
            monitor.print();

            socket.ReceiveTimeout = 50;

            if (logIn())
            { 
                Console.WriteLine("Success!");
                Console.WriteLine("Waiting for commands...");

                Command cmd = new Command();
                while (true)
                {
                    try
                    {
                        socket.Receive(cmd.Code);
                        Console.WriteLine("elo " + cmd.nCode);
                        if (cmd.nCode.Equals(255))  //exit
                        {
                            break;
                        }
                        if (cmd.interpret())        // check if command contains data
                        {
                            socket.Receive(cmd.Size);
                            cmd.setDataSize(cmd.Size);
                            socket.Receive(cmd.Data);
                        }
                        cmd.assignCmd(id);
                        switch (cmd.nCode)
                        {
                            case 6:     // download
                                monitor.queueDownload(cmd);
                                break;
                            case 7:     // upload
                                monitor.queueUpload(cmd);
                                break;
                            default:    // other
                                monitor.queueMisc(cmd);
                                break;
                        }

                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine("timeout");
                        if (readyToSend)
                        {
                            Console.WriteLine("wysylam");
                        }
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
            Console.WriteLine("CH exit");
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

    }
}
