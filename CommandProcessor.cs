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
        private String id = "";

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
            try
            {
                cmd = incoming.Take(token);
                if (logIn((CommandLogin)cmd))
                {
                    while (!token.IsCancellationRequested)
                    {
                        cmd = incoming.Take(token);
                        processCommand(cmd);
                    }
                }
            }
            catch (OperationCanceledException oce)
            {
                if(!token.IsCancellationRequested)
                    cts.Cancel();
            }
            

        }
        

        private void processCommand(Command cmd)
        {
            if (cmd.GetType().Equals(typeof(CommandAccept)))
            {
                toSend.Add(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandList)))
            {
                ((CommandList)cmd).CmdProc = this;
                monitor.queueMisc(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandDownload)))
            {
                ((CommandDownload)cmd).CmdProc = this;
                monitor.queueDownload(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandUpload)))
            {
                ((CommandUpload)cmd).CmdProc = this;
                monitor.queueUpload(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandRename)))
            {
                ((CommandRename)cmd).CmdProc = this;
                monitor.queueMisc(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandDelete)))
            {
                ((CommandDelete)cmd).CmdProc = this;
                monitor.queueMisc(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandChunk)))
            {
                if (((CommandChunk)cmd).SentByClient)
                    monitor.UploadChunkQueue.Add((CommandChunk) cmd);
                else
                    toSend.Add(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandError)))
            {
                Console.WriteLine("CH " + ID +": ERROR: " + ((CommandError)cmd).ErrorCode);
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
                Console.WriteLine("CH *new*: logging in...");
                Console.WriteLine("CH *new*: Login: {0} Pass: {1}", cmd.User, cmd.Pass);
                if (authenticate(cmd.User, cmd.Pass))
                {
                    ID = cmd.User;
                    Console.WriteLine("CH " + ID + ": Login successful");
                    Command ctmp = new CommandAccept();
                    ToSend.Add(ctmp);
                    return true;
                }
                else
                {
                    Console.WriteLine("CH *new*: Invalid login or password. Login failed");
                    Command ctmp = new CommandError(402);
                    ToSend.Add(ctmp);
                    Thread.Sleep(3000);
                    cts.Cancel();
                    return false;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("CH *new*: Login failed");
                cts.Cancel();
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
    }
}
