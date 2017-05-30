using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FAPS.Commands;
using System.Collections.Concurrent;

namespace FAPS
{
    class Scheduler
    {
        private Middleman monitor;
        private List <DataServerHandler> serverList = new List<DataServerHandler>();
        private bool dwnloading;
        private int lastFrag, maxFrag;
        private BlockingCollection <CommandDownload> failedFrags;
        private List <bool> succFrags;
        private NetworkFrame dlFile;
        private CancellationToken token;
        private CommandProcessor currentClient;
        private int fragSize;
        private int fileBufferSize;

        public int FragSize { get { return fragSize; } set { fragSize = value; } }
        public int FileBufferSize { get { return fileBufferSize; } set { fileBufferSize = value; } }

        public Scheduler(Middleman _monitor, CancellationToken _token)
        {
            monitor = _monitor;
            token = _token;
            dwnloading = false;
            fragSize = 1*8*1024;
            fileBufferSize = 10;

            startService();
        }

        private Task startService()
        {
            return Task.Factory.StartNew(run, token);
        }

        private List<Tuple<String, String>> loadServerList()
        {
            var tmpServerList = new List<Tuple<String, String>>();
            String[] result;
            String[] separators = { ":" };
            using (StreamReader fs = new StreamReader("servers.txt"))
            {
                String line = null;
                while ((line = fs.ReadLine()) != null)
                {
                    result = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    tmpServerList.Add(Tuple.Create<String, String>(result[0], result[1]));
                }
            }
            return tmpServerList;
        }

        public bool connectToServers()
        {
            var tmpServerList = loadServerList();
            DataServerHandler dataServer;
            foreach(Tuple<String, String> t in tmpServerList)
            {
                Console.WriteLine("SCHEDULER: " + t.Item1 + ":" + t.Item2);
                dataServer = new DataServerHandler(monitor, this, token, t.Item1, Int32.Parse(t.Item2));
                dataServer.startService();
                serverList.Add(dataServer);
            }
            return true;
        }

        public void run()
        {
            connectToServers();

            while (!token.IsCancellationRequested)
            {
                Command cmd = monitor.Fetch();
                processCommand(cmd);
            }
        }



        private void processCommand(Command cmd)
        {
            if (cmd.GetType().Equals(typeof(CommandDownload)))
            {
                startDownload((CommandDownload) cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandUpload)))
            {
                startUpload((CommandUpload) cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandList)))
            {
                send(cmd, serverList[0]);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandRename)))
            {
                sendEveryone(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandDelete)))
            {
                sendEveryone(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandError)))
            {
                Console.WriteLine("SH: ERROR: " + ((CommandError)cmd).ErrorCode);
                return;
            }
        }

        private void sendEveryone(Command cmd)
        {
            foreach (DataServerHandler server in serverList)      // rename, delete etc - pass to all servers
                send(cmd, server);
        }

        private void send(Command cmd, DataServerHandler server)
        {
            server.addCmd(cmd);
        }

        private void startDownload(CommandDownload cmd)
        {
            int working = 0;
            lastFrag = 0;
            int totalSize = cmd.End;
            maxFrag = totalSize / fragSize;
            succFrags = new List<bool>(maxFrag);
            int lastSucc = 0;
            for (int i = 0; i < maxFrag; i++)
                succFrags.Add(false);

            CommandDownload dwn;
            DataServerHandler server;
            while (true)
            {
                if (failedFrags.Count > 0)      // There are some fragments that need to be redownloaded
                {
                    server = avaibleServer();
                    if (server != null)
                    {
                        dwn = failedFrags.Take(token);
                        server.addDownload(dwn);
                    }
                }
                else if (lastFrag < maxFrag)    // Still missing few
                {
                    if (working <= fileBufferSize)
                    {
                        server = avaibleServer();
                        if (server != null)
                        {
                            dwn = makeChunk(cmd, lastFrag);
                            server.addDownload(dwn);
                            lastFrag++;
                            working++;
                        }
                    }
                    else
                        // Has the next fragment in order finished downloading?
                        for (int i = lastSucc + 1; i < fileBufferSize; i++)
                        {
                            if (succFrags[i] == true)
                            {
                                lastSucc = i;
                                working--;
                            }
                            else
                                break;
                        }
                }
                else if (allSucc())
                    // All fragments have started downloading and no one failed yet,
                    // so check if they all succeeded
                    break;
            }
        }

        private CommandDownload makeChunk(CommandDownload cmd, int frag)
        {
            CommandDownload newcmd = new CommandDownload(cmd);
            newcmd.Begin = frag * fragSize;
            newcmd.End = (frag + 1) * fragSize;
            if (newcmd.Begin > cmd.End || newcmd.Begin < 0)
                return null;
            if (newcmd.End > cmd.End)
                newcmd.End = cmd.End;
            return newcmd;
        }

        private void startUpload(CommandUpload cmd)
        {
            // Upload file on every server;
            foreach (DataServerHandler server in serverList)
                server.addUpload(cmd);
        }

        private DataServerHandler avaibleServer()
        {
            foreach (DataServerHandler server in serverList)
                if (server.State == DataServerHandler.States.idle)
                    return server;
            return null;
        }

        public void addFailed(CommandDownload cmd)
        {
            failedFrags.Add(cmd);
        }

        private bool allSucc()
        {
            for (int i = 0; i < maxFrag; i++)
                if (succFrags[i] == false)
                    return false;
            return true;
        }

        public void success(int fragment)
        {
            // Server handlers will invoke this upon succesfull download
            succFrags[fragment] = true;
        }
    }
}
