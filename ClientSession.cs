using System;
using System.Collections.Generic;
using System.IO;

namespace FAPS
{
    class ClientSession
    {
        private string id;
        public enum STATE { unauthenticated , logged, waiting, working };

        public string ID
        {
            get { return id; }
            set { id = value; }
        }

        public STATE State
        {
            get { return state; }
            set { state = value; }
        }

        private STATE state = STATE.unauthenticated;

        public void authenticate(String login, String pass)
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
                state = STATE.logged;
        }
    }
}
