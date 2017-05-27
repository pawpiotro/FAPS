using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FAPS.Commands;

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
                    if (!(cmd.GetType().Equals(typeof(CommandLogin))))
                    {
                        Console.WriteLine("CH: Unexpected command");
                        continue;
                    }
                }
                catch (OperationCanceledException oce)
                {
                    break;//
                }
            } while (!logIn((CommandLogin)cmd));
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
            if (cmd.GetType().Equals(typeof(CommandList)))
            {
                monitor.queueMisc(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandDownload)))
            {
                monitor.queueDownload(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandUpload)))
            {
                monitor.queueUpload(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandRename)))
            {
                monitor.queueMisc(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandDelete)))
            {
                monitor.queueMisc(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandChunk)))
            {
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandError)))
            {
                Console.WriteLine("CH: ERROR: " + ((CommandError)cmd).ErrorCode);
                cts.Cancel();
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandExit)))
            {
                cts.Cancel();
                return;
            }
        }

        private bool logIn(CommandLogin cmd)
        {            
            try
            {
                Console.WriteLine("CH: logging in...");
                Console.WriteLine("CH: Login: {0} Pass: {1}", cmd.User, cmd.Pass);
                if (authenticate(cmd.User, cmd.Pass))
                {
                    Console.WriteLine("CH: Login successful");
                    ID = cmd.User;
                    Command ctmp = new CommandAccept();
                    ToSend.Add(ctmp);
                    return true;
                }
                else
                {
                    Console.WriteLine("CH: Login failed");
                    return false;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("CH: COMMAND LOGIN BUILD: FAILED");
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
