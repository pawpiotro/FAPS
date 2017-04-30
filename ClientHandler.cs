using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAPS
{
    class ClientHandler
    {
        Monitor monitor;

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

        public ClientHandler(Monitor _monitor){
            monitor = _monitor;
            monitor.inc();
            monitor.print();
            if (authenticate("user1", "pass1"))
                Console.WriteLine("elo");
            else
                Console.WriteLine("nie elo");
        }

    }
}
