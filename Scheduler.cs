using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FAPS
{
    class Scheduler
    {
        private Middleman monitor;
        private List <DataServerHandler> ServerList; // TODO change string to other type
        private bool dwnloading;
        private int lastFrag, maxFrag;
        private Queue <int> failedFrags;
        private List <bool> SuccFrags;
        private Command dlFile;
        private object cmdlock = new object();

        public Scheduler(Middleman _monitor)
        {
            monitor = _monitor;
            monitor.print();
            dwnloading = false;
        }

        private List<Tuple<String, String>> loadServerList()
        {
            var serverList = new List<Tuple<String, String>>();
            String[] result;
            String[] separators = { ":" };
            using (StreamReader fs = new StreamReader("servers.txt"))
            {
                String line = null;
                while ((line = fs.ReadLine()) != null)
                {
                    result = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    serverList.Add(Tuple.Create<String, String>(result[0], result[1]));
                }
            }
            return serverList;
        }

        public bool connectToServers()
        {
            var serverList = loadServerList();
            DataServerHandler dataServer;
            foreach(Tuple<String, String> t in serverList)
            {
                dataServer = new DataServerHandler(monitor, this, t.Item1, Int32.Parse(t.Item2), cmdlock);
                Thread dataServerThread = new Thread(dataServer.run);
                dataServerThread.Start();
            }
            return true;
        }

        public void Success(int fragment)
        {
            // thready beda to wywolywac przy pomyslnym pobraniu fragmentu
            SuccFrags[fragment] = true;
        }

        // Methods creating networking threads for specific purpose
        private void Download(Command file, int fragment, DataServerHandler server)
        {
            //create ServerHandler(download, file, fragment, this, sever)
        }

        private void Upload(Command file, DataServerHandler server)
        {
            //create ServerHandler(upload, file, this)
        }

        private void Command(Command cmd, DataServerHandler server)
        {
            lock (cmdlock)
            {
                server.Addcmd(cmd);
                Monitor.Pulse(cmdlock);
            }
        }

        public void run()
        {
            /*   wez komendy
                zrob komendy
                profit
            */
            while(true)
            {
                if (!dwnloading && monitor.dlReady())
                {
                    // Someone's waiting for a download, start dl
                    dwnloading = true;
                    lastFrag = 0;
                    dlFile = monitor.dlFetch();
                    maxFrag = monitor.dlSize();
                    SuccFrags = new List <bool> (maxFrag);
                    for (int i = 0; i < maxFrag; i++)
                        SuccFrags.Add(false);
                }
                else if (dwnloading)
                {
                    // Check if file is downloaded
                    if (lastFrag > maxFrag && failedFrags.Count == 0)   // Any fragments left to download?
                    {
                        bool done = true;
                        for (int i = 0; i < maxFrag; i++)
                            if (SuccFrags[i] == false)
                                done = false;
                        if (done)   // Did every fragment finished downloading?
                            dwnloading = false;
                    }
                    else
                    {
                        // Look through the servers list and start download form idle ones
                        for (int i = 0; i < ServerList.Count; i++)
                        {
                            DataServerHandler server = ServerList[i];
                            if (!server.busy)
                                if (failedFrags.Count > 0)   // At least one fragment has to be redownloaded
                                {
                                    Download(dlFile, failedFrags.Dequeue(), server);
                                    server.busy = true;
                                }
                                else if (lastFrag <= maxFrag)
                                {
                                    Download(dlFile, lastFrag, server);
                                    server.busy = true;
                                    lastFrag++;
                                }
                        }
                     }
                }
                if (monitor.ulReady())
                {
                    // Upload file on every server;
                    Command ulFile = monitor.ulFetch();
                    foreach (DataServerHandler server in ServerList)
                        Upload(ulFile, server);
                }
                if (monitor.cmdReady())
                {
                    Command cmd = monitor.cmdFetch();
                    /*if (cmd = FILELIST)     // Get the list from one server (since they all share same files)
                        Command(monitor.cmdfetch(), ServerList[0]);
                    else*/
                        foreach (DataServerHandler server in ServerList)      // rename, delete etc - pass to all servers
                            Command(cmd, server);
                }
            }
        }
    }
}
