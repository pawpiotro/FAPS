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
        private static object dshLock = new object();
        private static object schLock = new object();
        private int lastFrag, maxFrag, lastSucc, fragSize, fileBufferSize;
        private BlockingCollection <CommandDownload> failedFrags = new BlockingCollection<CommandDownload>();
        private List <bool> succFrags;
        private byte[] uplFrags;
        private CommandChunk[] uplBuff;
        private CancellationToken token;
        private CommandProcessor currentClient;
        private CommandCommit commit = null;
        private int waitingForCommit = 0, commited = 0, accepted = 0;

        public int FragSize { get { return fragSize; } set { fragSize = value; } }
        public int FileBufferSize { get { return fileBufferSize; } set { fileBufferSize = value; } }

        public Scheduler(Middleman _monitor, CancellationToken _token)
        {
            monitor = _monitor;
            token = _token;
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

        private CommandDownload makeChunk(CommandDownload cmd, int frag)
        {
            Console.WriteLine("Tworze nowy chunk");
            CommandDownload newcmd = new CommandDownload(cmd);
            //CommandDownload newcmd = cmd;
            Console.WriteLine("Download dla "+cmd.User +"Chunk dla " + newcmd.User);
            newcmd.Begin = frag * fragSize;
            newcmd.End = (frag + 1) * fragSize;
            if (newcmd.Begin > cmd.End || newcmd.Begin < 0)
                return null;
            if (newcmd.End > cmd.End)
                newcmd.End = cmd.End;
            Console.WriteLine("Stworzono chunk");
            return newcmd;
        }

        private void startDownload(CommandDownload cmd)
        {
            currentClient = cmd.CmdProc;
            lastFrag = 0;
            int totalSize = cmd.End;
            maxFrag = (totalSize + fragSize - 1) / fragSize; // Round up
            monitor.setDownloadBuffer(fileBufferSize);
            succFrags = new List<bool>(maxFrag);
            lastSucc = 0;
            for (int i = 0; i < maxFrag; i++)
                succFrags.Add(false);

            CommandDownload dwn;
            DataServerHandler server;
            Console.WriteLine("Zaczynam Download");
            while (true)
            {
                server = avaibleServer();
                if (failedFrags.Count > 0)      // There are some fragments that need to be redownloaded
                {
                    Console.WriteLine("Zfailowany fragment");
                    dwn = failedFrags.Take(token);
                    server.addDownload(dwn, dwn.Begin / fragSize);
                }
                else if (lastFrag < maxFrag)    // Still missing few
                {
                    if (lastFrag >= lastSucc + fileBufferSize)
                    {
                        waitForDwn();
                        continue;
                    }
                    Console.WriteLine("Pobieram nowy chunk");
                    dwn = makeChunk(cmd, lastFrag);
                    server.addDownload(dwn, lastFrag);
                    lastFrag++;
                }
                else if (allSucc())
                {
                    // All fragments have started downloading and no one failed yet,
                    // so check if they all succeeded
                    Console.WriteLine("Pobieranie zakonczone");
                    break;
                }
            }
        }

        private void startUpload(CommandUpload cmd)
        {
            // Upload file on every server
            Console.WriteLine("Rozpoczynam upload");
            foreach (DataServerHandler server in serverList)
                server.addUpload(cmd);      // Notify every server about upload
            lock(schLock)
            {
                while (accepted < serverList.Count)
                    Monitor.Wait(schLock);
            }
            Console.WriteLine("Zaakceptowano upload na wszystkich serwerach");


            accepted = 0;
            cmd.CmdProc.Incoming.Add(new CommandAccept(), token);
            uplBuff = new CommandChunk[fileBufferSize];
            maxFrag = (int)((cmd.Size + fragSize - 1)/ fragSize); // Round up
            uplFrags = new byte[maxFrag];
            lastSucc = 0;
            for (int i = 0; i < maxFrag; i++)
                uplFrags[i] = 0;
            for (int i = 0; i < fileBufferSize && i < maxFrag; i++)
            {
                uplBuff[i] = monitor.UploadChunkQueue.Take(token);  // Ready chunks queue
            }
            lock(dshLock)
            {
                Monitor.PulseAll(dshLock);
            }

            
            // Start uploading chunks
            while (true)
            {
                lock (schLock)
                {
                    Console.WriteLine("#############Ide spac 1");
                    Monitor.Wait(schLock);
                    Console.WriteLine("Obudzony 1");
                    if (lastSucc >= maxFrag)
                    {
                        // All chunks taken by DSHs, wait for all CommitRdys
                        while (waitingForCommit < serverList.Count)
                            Monitor.Wait(schLock);
                        commit = new CommandCommit();
                        Console.WriteLine("Commit gotowy");
                        lock(dshLock)
                            Monitor.PulseAll(dshLock);
                        // Submit commit, wait for all CommitAcks
                        while (commited < serverList.Count)
                            Monitor.Wait(schLock);
                        waitingForCommit = 0;
                        commited = 0;
                        commit = null;
                        Console.WriteLine("Zuploadowano plik na kazdy serwer");
                        cmd.CmdProc.Incoming.Add(new CommandAccept(), token);
                        break;
                    }
                    lock (dshLock)
                    {
                        if ((int)uplFrags[lastSucc] == serverList.Count)
                        {
                            Console.WriteLine("wszystkie zassały chunka");
                            // All DSHs uploaded one chunk, swap it with a new one
                            uplFrags[lastSucc]++;
                            if (lastSucc < maxFrag - 1)
                                uplBuff[lastSucc % fileBufferSize] = monitor.UploadChunkQueue.Take(token);
                            lastSucc++;
                        }
                        Console.WriteLine("#############Ide spac 2");
                        Monitor.PulseAll(dshLock);
                        Console.WriteLine("Obudzony 2");
                    }
                }
            }
        }

        private DataServerHandler avaibleServer()
        {
            while (true)
            {
                foreach (DataServerHandler server in serverList)
                    if (server.State == DataServerHandler.States.idle)
                        return server;
                waitForDwn();
            }
        }

        public void addFailed(CommandDownload cmd)
        {
            failedFrags.Add(cmd);
        }

        private bool allSucc()
        {
            for (int i = 0; i < maxFrag; i++)
                if (succFrags[i] == false)
                {
                    waitForDwn();
                    return false;
                }
            return true;
        }

        public void success(int fragment)
        {
            // Server handlers will invoke this upon succesfull download
            succFrags[fragment] = true;
        }

        public CommandChunk takeUplChunk(int frag)
        {
            lock(dshLock)
            {
                // Give DSH next chunk to upload
                int min = frag - fileBufferSize;
                if (min < 0)
                    return uplBuff[frag];
                // Chunk index is bigger than buffer size, so check if it's in buffer
                while (true)
                    if ((int)uplFrags[min] == serverList.Count + 1)
                        return uplBuff[frag % fileBufferSize];
                    else
                        Monitor.Wait(dshLock);
            }
        }

        public void uplSucc(int frag)
        {
            lock(dshLock)
            {
                uplFrags[frag]++;
            }
        }

        private void waitForDwn()
        {
            lock (schLock)
            {
                Monitor.Wait(schLock);
                // Has the next fragment in order finished downloading?
                for (int i = lastSucc; i < fileBufferSize; i++)
                {
                    if (succFrags[i] == true)
                    {
                        lastSucc = i + 1;
                        currentClient.Incoming.Add(monitor.takeDownloadChunk(i), token);
                    }
                    else
                        break;
                }
            }
        }

        private void waitForUpl()
        {
            lock (schLock)
            {
                Monitor.Wait(schLock);
            }
        }

        public CommandCommit waitForCommit()
        {
            lock(dshLock)
            {
                waitingForCommit++;
                wakeSch();
                while (commit == null)
                    Monitor.Wait(dshLock);
                return commit;
            }
        }
        public void ConfirmCommit()
        {
            lock(dshLock)
            {
                commited++;
                wakeSch();
            }
        }
        public void ConfirmAccept()
        {
            lock(dshLock)
            {
                accepted++;
                wakeSch();
                Monitor.Wait(dshLock);
            }
        }

        public void wakeSch()
        {
            lock (schLock)
            {
                Monitor.Pulse(schLock);
            }
        }
        public void wakeDsh()
        {
            lock (dshLock)
            {
                Monitor.Pulse(dshLock);
            }
        }

    }
}
