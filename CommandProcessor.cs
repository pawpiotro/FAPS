using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FAPS
{
    class CommandProcessor
    {
        private CancellationTokenSource cts;
        private CancellationToken token;
        private BlockingCollection<Command> incoming = new BlockingCollection<Command>();
        private BlockingCollection<Command> toSend = new BlockingCollection<Command>();
        private Middleman monitor;
        private String id;

        public BlockingCollection<Command> ToSend
        {
            get { return toSend; }
            set { toSend = value; }
        }

        public BlockingCollection<Command> Incoming
        {
            get { return incoming; }
            set { incoming = value; }
        }

        public String ID
        {
            get { return id; }
            set { id = value; }
        }

        public CommandProcessor(Middleman _monitor, CancellationTokenSource _cts)
        {
            monitor = _monitor;
            cts = _cts;
            token = cts.Token;

            startThread();
        }

        private void startThread()
        {
            Task.Factory.StartNew(run, token);
        }

        private void run()
        {
            Command cmd = null;
            do
            {
                try
                {
                    cmd = incoming.Take(token);
                }
                catch (OperationCanceledException oce)
                {
                    //
                }
            } while (!logIn(cmd));
            // LOGGED IN
            while (!token.IsCancellationRequested)
            {
                try
                {
                    cmd = incoming.Take(token);
                    processCommand(cmd);
                }
                catch (OperationCanceledException oce)
                {
                    //
                }
            }
        }
        

        private void processCommand(Command cmd)
        {
            switch ((Command.CMD)cmd.nCode)
            {
                case Command.CMD.LIST:
                    monitor.queueMisc(cmd);
                    break;
                case Command.CMD.DOWNLOAD:
                    monitor.queueDownload(cmd);
                    break;
                case Command.CMD.UPLOAD:
                    monitor.queueUpload(cmd);
                    break;
                case Command.CMD.CHUNK:
                    break;
                case Command.CMD.DELETE:
                    monitor.queueMisc(cmd);
                    break;
                case Command.CMD.RENAME:
                    monitor.queueMisc(cmd);
                    break;
                case Command.CMD.ERROR:
                    ERROR();
                    break;
                case Command.CMD.EXIT:
                    EXIT();
                    break;
                default:
                    break;
            }
        }

        private bool logIn(Command cmd)
        {
            try
            {
                Console.WriteLine("CH: logging in...");
                char[] separators = { ':' };
                String[] tmp = cmd.sData.Split(separators);
                Console.WriteLine("CH: Login: {0} Pass: {1}", tmp[0], tmp[1]);
                if (authenticate(tmp[0], tmp[1]))
                {
                    Console.WriteLine("CH: Login successful");
                    ID = tmp[0];
                    Command ctmp = new Command(Command.CMD.ACCEPT);
                    ToSend.Add(ctmp);
                    return true;
                }
                else
                {
                    Console.WriteLine("CH: Login failed");
                    return false;
                }
            }
            catch (NullReferenceException)
            {
                return false;
            }
          
        }

        public bool authenticate(String login, String pass)
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

        private void EXIT()
        {
            cts.Cancel();
        }

        private void ERROR()
        {
            Console.WriteLine("CH: ERROR");
            cts.Cancel();
        }

   
    }
}
