using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAPS
{
    class ClientHandler
    {
        public static int i = 0;
        Monitor monitor;
        Socket socket;
        Boolean readyToSend = true;

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

        public void run()
        {

            monitor.inc();
            monitor.print();

            socket.ReceiveTimeout = 500;

            Command cmd = new Command();
            cmd.nCode = 8;
            
            socket.Send(cmd.Code);
            socket.Receive(cmd.Code);
            Console.WriteLine(cmd.Code);
            Console.WriteLine(cmd.nCode);
            if (cmd.nCode.Equals(2))
            {
                Console.WriteLine("logging in...");
                socket.Receive(cmd.Size);
                Console.WriteLine(cmd.nSize);
                cmd.setDataSize(cmd.Size);
                socket.Receive(cmd.Data);
                Console.WriteLine(cmd.Data);
                Console.WriteLine(cmd.sData);
                String tmp = cmd.sData;
                char[] separators = { ':' };
                String[] tmp2 = tmp.Split(separators);
                Console.WriteLine("Login: {0} Pass XD: {1}", tmp2[0], tmp2[1]);
                if (authenticate(tmp2[0], tmp2[1]))
                {
                    Console.WriteLine("Success");
                    Console.WriteLine("Waiting for commands...");
                    while (true)
                    {
                        try
                        {
                            socket.Receive(cmd.Code);
                            Console.WriteLine("Text received : {0}", cmd.nCode);
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine("timeout");
                            if (readyToSend)
                            {
                                Console.WriteLine("wysylam");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed");
                }
            }
            else Console.WriteLine("NIEE");
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

    }
}
