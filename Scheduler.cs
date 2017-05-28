using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FAPS.Commands;

namespace FAPS
{
    class Scheduler
    {
        private Middleman monitor;
        private List<DataServerHandler> serverList = new List<DataServerHandler>();
        private bool dwnloading;
        private int lastFrag, maxFrag;
        private Queue <int> failedFrags;
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

        public void success(int fragment)
        {
            // thready beda to wywolywac przy pomyslnym pobraniu fragmentu
            succFrags[fragment] = true;
        }

        private void download(Command file, int fragment, DataServerHandler server)
        {
            //create ServerHandler(download, file, fragment, this, sever)
        }

        private void upload(Command file, DataServerHandler server)
        {
            //create ServerHandler(upload, file, this)
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
            dwnloading = true;
            int working = 0;
            lastFrag = 0;
            int totalSize = cmd.End;
            maxFrag = totalSize / fragSize;
            succFrags = new List<bool>(maxFrag);
            for (int i = 0; i < maxFrag; i++)
                succFrags.Add(false);
            
            while (dwnloading)
            {
                while (lastFrag < maxFrag)
                {
                    if (working <= fileBufferSize)
                        ;
                    else
                        ;
                }
            }
            // Check if file is downloaded
            if (lastFrag > maxFrag && failedFrags.Count == 0)   // Any fragments left to download?
            {
                bool done = true;
                for (int i = 0; i < maxFrag; i++)
                    if (succFrags[i] == false)
                        done = false;
                if (done)   // Did every fragment finished downloading?
                    dwnloading = false;
            }
            else
            {
                // Look through the servers list and start download form idle ones
                for (int i = 0; i < serverList.Count; i++)
                {
                    DataServerHandler server = serverList[i];
                    if (!server.busy)
                        if (failedFrags.Count > 0)   // At least one fragment has to be redownloaded
                        {
                            download(dlFile, failedFrags.Dequeue(), server);
                            server.busy = true;
                        }
                        else if (lastFrag <= maxFrag)
                        {
                            download(dlFile, lastFrag, server);
                            server.busy = true;
                            lastFrag++;
                        }
                }
            }
        }

        private void startUpload(CommandUpload cmd)
        {
            // Upload file on every server;
            sendEveryone(cmd);
            foreach (DataServerHandler server in serverList)
                server.addUpload(cmd);
        }
    }
}
