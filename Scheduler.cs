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
        private int lastFrag, maxFrag, lastSucc, fragSizeDwn, fragSizeUpl, fileBufferSize;
        private BlockingCollection <CommandDownload> failedFrags = new BlockingCollection<CommandDownload>();
        private bool[] succFrags;
        private byte[] uplFrags;
        private CommandChunk[] uplBuff;
        private CancellationTokenSource linkedsrc, dshtksrc = new CancellationTokenSource();
        private CancellationToken proxytoken, dshtoken, token;
        private CommandProcessor currentClient;
        private CommandCommit commit = null;
        private int waitingForCommit = 0, commited = 0, accepted = 0;

        public int FragSizeDwn { get { return fragSizeDwn; } set { fragSizeDwn = value; } }
        public int FragSizeUpl { get { return fragSizeUpl; } set { fragSizeUpl = value; } }
        public int FileBufferSize { get { return fileBufferSize; } set { fileBufferSize = value; } }

        public Scheduler(Middleman _monitor, CancellationToken _token)
        {
            monitor = _monitor;
            proxytoken = _token;
            dshtoken = dshtksrc.Token;
            linkedsrc = CancellationTokenSource.CreateLinkedTokenSource(proxytoken, dshtoken);
            token = linkedsrc.Token;
            fragSizeDwn = 1*1024*2;
            fragSizeUpl = 1*1024*4;
            fileBufferSize = 10;

            startService();
        }

        private Task startService()
        {
            return Task.Factory.StartNew(run, token);
        }

        private List<Tuple<String, String>> loadServerList()
        {
            // Load servers IP from servers.txt file
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
            // Connect one DSH to each server
            var tmpServerList = loadServerList();
            DataServerHandler dataServer;
            int i = 0;
            foreach(Tuple<String, String> t in tmpServerList)
            {
                dataServer = new DataServerHandler(monitor, this, token, t.Item1, Int32.Parse(t.Item2), i++);
                dataServer.startService();
                serverList.Add(dataServer);
            }
            return true;
        }

        public void run()
        {
            CancellationTokenRegistration ctr = proxytoken.Register(CancelAsync);
            connectToServers();

            while (!proxytoken.IsCancellationRequested)
            {
                try
                {
                    // Get command from Middleman and process it
                    waitingForCommit = 0;
                    commited = 0;
                    commit = null;
                    accepted = 0;
                    Command cmd = monitor.Fetch();
                    processCommand(cmd);
                }
                catch(NullReferenceException nre)
                {
                    //TEMP
                    break;
                }
            }
            ctr.Dispose();
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
                startOther(cmd);
                return;
            }
            if (cmd.GetType().Equals(typeof(CommandDelete)))
            {
                startOther(cmd);
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
            // Make new Download command for one fragment
            Console.WriteLine("SCH: Tworze nowy chunk " +frag);
            CommandDownload newcmd = new CommandDownload(cmd);
            newcmd.Begin = frag * fragSizeDwn;
            newcmd.End = (frag + 1) * fragSizeDwn;
            if (newcmd.Begin > cmd.End || newcmd.Begin < 0)
                return null;
            if (newcmd.End > cmd.End)
                newcmd.End = cmd.End;
            Console.WriteLine("SCH: Chunk " + frag + " od " + newcmd.Begin + " do " + newcmd.End);
            return newcmd;
        }

        private void startDownload(CommandDownload cmd)
        {
            currentClient = cmd.CmdProc;
            lastFrag = 0;
            int totalSize = cmd.End;
            maxFrag = (totalSize + fragSizeDwn - 1) / fragSizeDwn; // Round up
            monitor.setDownloadBuffer(fileBufferSize);
            succFrags = new bool[maxFrag];
            lastSucc = 0;
            for (int i = 0; i < maxFrag; i++)
                succFrags[i] = false;

            CommandDownload dwn;
            DataServerHandler server;
            Console.WriteLine("SCH: Zaczynam Download");
            while (!token.IsCancellationRequested)
            {
                // Wait for first idle DSH
                server = avaibleServer();
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (failedFrags.Count > 0)      // There are some fragments that need to be redownloaded
                {
                    dwn = failedFrags.Take(token);
                    Console.WriteLine("SCH: Zfailowany fragment " + dwn.Begin);
                    server.addDownload(dwn, dwn.Begin / fragSizeDwn);
                }

                else if (lastFrag < maxFrag)    // Not all fragments have been downloaded
                {
                    if (lastFrag >= lastSucc + fileBufferSize)
                    {
                        Console.WriteLine("SCH: Czekam na pobranie " + lastSucc);
                        waitForDwn();
                        continue;
                    }
                    // Make new Download command for specific fragment and pass it to DSH
                    dwn = makeChunk(cmd, lastFrag);
                    Console.WriteLine("SCH: Zlecam nowy chunk " + lastFrag +" "+ dwn.Begin+" "+dwn.End);
                    server.addDownload(dwn, lastFrag);
                    lastFrag++;
                }

                // All fragments have started downloading and no one failed yet,
                // so check if they all succeeded
                else if (allSucc())
                {
                    Console.WriteLine("SCH: Pobieranie zakonczone");
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
                // Wait for Accept from every server
                while (accepted < serverList.Count)
                    Monitor.Wait(schLock);
            }
            Console.WriteLine("Zaakceptowano upload na wszystkich serwerach");

            // Prepare upload chunks for DSHs and wake them up
            cmd.CmdProc.Incoming.Add(new CommandAccept(), token);
            uplBuff = new CommandChunk[fileBufferSize];
            maxFrag = (int)((cmd.Size + fragSizeUpl - 1)/ fragSizeUpl); // Round up
            uplFrags = new byte[maxFrag];
            lastSucc = 0;
            for (int i = 0; i < maxFrag; i++)
                uplFrags[i] = 0;
            for (int i = 0; i < fileBufferSize && i < maxFrag && !token.IsCancellationRequested; i++)
            {
                Console.WriteLine("SCH: Wstawiam do kolejki chunka " + i);
                uplBuff[i] = monitor.UploadChunkQueue.Take(token);  // Ready chunks queue
            }
            Console.WriteLine("SCH: maxfrag = " + maxFrag + " caly size = " + cmd.Size);
            lock (dshLock)
            {
                Monitor.PulseAll(dshLock);
            }

            
            // Start uploading chunks
            while (!token.IsCancellationRequested)
            {
                try
                {
                    lock (schLock)
                    {
                        // Wait for at least one DSH to complete/fail upload of one chunk
                        Monitor.Wait(schLock);

                        // Check if all chunks have been uploaded
                        if (lastSucc >= maxFrag)
                        {
                            // All chunks taken by DSHs, wait for all CommitRdys
                            while (waitingForCommit < serverList.Count && !token.IsCancellationRequested)
                                Monitor.Wait(schLock);
                            if (token.IsCancellationRequested)
                            {
                                break;
                            }
                            commit = new CommandCommit();
                            Console.WriteLine("Commit gotowy");
                            lock (dshLock)
                                Monitor.PulseAll(dshLock);

                            // Submit commit, wait for all CommitAcks
                            while (commited < serverList.Count && !token.IsCancellationRequested)
                                Monitor.Wait(schLock);
                            if (token.IsCancellationRequested)
                            {
                                break;
                            }
                            Console.WriteLine("Zuploadowano plik na kazdy serwer");
                            cmd.CmdProc.Incoming.Add(new CommandAccept(), token);
                            break;
                        }


                        // Check if one chunk has been uploaded by all DSHs
                        lock (dshLock)
                        {
                            if ((int)uplFrags[lastSucc] == serverList.Count)
                            {
                                Console.WriteLine("SCH: Wszystkie zassały chunka " + lastSucc);
                                // All DSHs uploaded one chunk, swap it with a new one
                                uplFrags[lastSucc]++;
                                if (lastSucc < maxFrag && maxFrag - lastSucc > fileBufferSize)
                                {
                                    Console.WriteLine("SCH: Wstawiam chunka na miejsce " + lastSucc % fileBufferSize);
                                    uplBuff[lastSucc % fileBufferSize] = monitor.UploadChunkQueue.Take(token);
                                }
                                lastSucc++;
                            }
                            Monitor.PulseAll(dshLock);
                        }
                    }
                }
                catch(Exception e)
                {
                    //TEMP
                }
            }
            // Token is cancelled
        }

        private void startOther(Command cmd)
        {
            // Pass command to every DHSs
            sendEveryone(cmd);
            if (!cmd.GetType().Equals(typeof(CommandRename)) && !cmd.GetType().Equals(typeof(CommandDelete)))
            {
                Console.WriteLine("Nieprawidlowy command");
                return;
            }
            lock (schLock)
            {
                // Wait for CommitRdy from all servers
                while (waitingForCommit < serverList.Count && !token.IsCancellationRequested)
                    Monitor.Wait(schLock);
                if (token.IsCancellationRequested)
                {
                    return;
                }
                commit = new CommandCommit();
                Console.WriteLine("Commit gotowy");
                lock (dshLock)
                    Monitor.PulseAll(dshLock);
                // Submit commit, wait for all CommitAcks
                while (commited < serverList.Count && !token.IsCancellationRequested)
                    Monitor.Wait(schLock);
                if (token.IsCancellationRequested)
                {
                    return;
                }
                waitingForCommit = 0;
                commited = 0;
                commit = null;
                Console.WriteLine("Dostarczono komende na kazdy serwer");
                if (cmd.GetType().Equals(typeof(CommandRename)))
                    ((CommandRename)cmd).CmdProc.Incoming.Add(new CommandAccept(), token);
                else if (cmd.GetType().Equals(typeof(CommandDelete)))
                    ((CommandDelete)cmd).CmdProc.Incoming.Add(new CommandAccept(), token);
            }
        }

        private DataServerHandler avaibleServer()
        {
            // Check for the first idle server
            while (!token.IsCancellationRequested)
            {
                foreach (DataServerHandler server in serverList)
                    if (server.State == DataServerHandler.States.idle)
                        return server;
                waitForDwn();
            }
            return null;
        }

        public void addFailed(CommandDownload cmd)
        {
            // Used by DSH to return a failed Download command
            failedFrags.Add(cmd);
        }

        private bool allSucc()
        {
            // Check if all fragments have beed succesfully downloaded
            lock(schLock)
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
            // DSHs will invoke this upon succesfull download
            lock(schLock)
            {
                Console.WriteLine("Succes chunka " + fragment);
                succFrags[fragment] = true;
            }
        }

        public CommandChunk takeUplChunk(int frag, int id)
        {
            // Used by DSH, wait for next chunk to upload
            Console.WriteLine("DSH" + id + " Biore chunka " + frag);
            lock (dshLock)
            {
                // Give DSH next chunk to upload
                int min = frag - fileBufferSize;
                if (min < 0)
                    return uplBuff[frag];
                // Chunk index is bigger than buffer size
                // so check if the new chunk has already been swapped with a previous one in buffer
                while (!token.IsCancellationRequested)
                    if ((int)uplFrags[min] == serverList.Count + 1)
                        return uplBuff[frag % fileBufferSize];
                    else
                        Monitor.Wait(dshLock);
                return null;
            }
        }

        public void uplSucc(int frag)
        {
            // Used by DSH when chunk is succesfully uploaded
            lock(dshLock)
            {
                uplFrags[frag]++;
            }
        }

        private void waitForDwn()
        {
            // Wait for chunk next in order to download, then send it to client
            lock (schLock)
            {
                Monitor.Wait(schLock);
                if (token.IsCancellationRequested)
                    return;
                // Has the next fragment in order finished downloading?
                for (int i = lastSucc; i < maxFrag; i++)
                {
                    if (succFrags[i] == true)
                    {
                        lastSucc = i + 1;
                        Console.WriteLine("SCH: Wysyłam do klienta fragment " + lastSucc);
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
            // Used by DSH to signal it's ready and waiting for commit
            lock(dshLock)
            {
                waitingForCommit++;
                wakeSch();
                while (commit == null && !token.IsCancellationRequested)
                    Monitor.Wait(dshLock);
                return commit;
            }
        }
        public void ConfirmCommit()
        {
            // Used by DSH to confirm CommitAck from server
            lock(dshLock)
            {
                commited++;
                wakeSch();
            }
        }
        public void ConfirmAccept(int id)
        {
            // Used by DSH to confirm Accept from server
            lock(dshLock)
            {
                Console.WriteLine("DSH" + id + " Czekam na accepty innych serwerow");
                accepted++;
                wakeSch();
                do
                {
                    Monitor.Wait(dshLock);
                    Console.WriteLine("DSH" + id + " Accepted " + accepted);
                }
                while (accepted < serverList.Count);
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
        public void wakeAllDsh()
        {
            lock (dshLock)
            {
                Monitor.PulseAll(dshLock);
            }
        }

        public void cancel()
        {
            // Used by DSH to cancel other DSHs operations
            foreach (DataServerHandler server in serverList)
                server.cancel();
            dshtksrc.Cancel();
            wakeSch();
            wakeAllDsh();
        }
        public void CancelAsync()
        {
            Console.WriteLine("SCH: CANCEL");
            foreach (DataServerHandler server in serverList)
                server.disconnect();
        }
    }
}
