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
        Boolean readyToSend = false;

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
            /*
            for(int i = 0; i < list.Count(); i++)
            {
                Console.WriteLine("Login: {0} Pass XD: {1}", list[i].Item1, list[i].Item2);
            }*/
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
            byte[] sdata = new byte[1024];
            socket.Send(sdata);
            monitor.inc();
            monitor.print();

            String data = null;
            byte[] bytes = new byte[1024];

            socket.ReceiveTimeout = 500;
            
            byte[] msg = { 8 };
            socket.Send(msg);
            Console.WriteLine("logging in...");
            int bytesRec = socket.Receive(bytes);
            data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
            // jak login bedzie wygladal i haslo?
            // data split
            if (authenticate("user1", "pass1"))
            {
                Console.WriteLine("Succes");
                while (true)
                {
                    try
                    {
                        bytesRec = socket.Receive(bytes);
                        data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        Console.WriteLine("Text received : {0}", data);
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
            } else
            {
                Console.WriteLine("Failed");
            }
            
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

    }
}
