using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FAPS
{
    class Scheduler
    {
        private Monitor monitor;
        // ServerList
        private bool dwnloading;
        private int lastFrag, maxFrag;
        //fifo failedFrags;
        //bool SuccFrags[];

        public Scheduler(Monitor _monitor)
        {
            monitor = _monitor;
            monitor.print();
            dwnloading = false;
            /*
            for (i in SuccFrags)
                SuccFrags[i] = false;
            */
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
                dataServer = new DataServerHandler(monitor, t.Item1, Int32.Parse(t.Item2));
                Thread dataServerThread = new Thread(dataServer.run);
                dataServerThread.Start();
            }
            return true;
        }

        public void Success(int fragment)
        {
            // thready beda to wywolywac przy pomyslnym pobraniu fragmentu
            //SuccFrags[fragment] = true;
        }

        // Methods creating networking threads for specific purpose
        private void Download(string file, int fragment, int server)
        {
            //create ServerHandler(download, file, fragment, this, sever)
        }

        private void Upload(string file, int server)
        {
            //create ServerHandler(upload, file, this)
        }

        private void Command(string cmd, int server)
        {
            //create ServerHandler(command, cmd, this)
        }

        public void run()
        {
            /*   wez komendy
                zrob komendy
                profit
            */
            while(true)
            {/*
                if (!dwnloading && monitor.dlReady())
                {
                    // Someone's waiting for a download, start dl
                    dwnloading = true;
                    lastFrag = 0;
                    dlFile = monitor.dlfetch();
                    maxFrag = monitor.dlsize();
                    SuccFrags.setsize(maxFrag);
                }
                else if (dwnloading)
                {
                    // Check if file is downloaded
                    if (lastFrag > maxFrag && failedFrag.empty())   // Any fragments left to download?
                    {
                        bool done = true;
                        for (i in SuccFrags)
                            if (SuccFrags[i] = false)
                                done = false;
                        if (done)   // Did every fragment finished downloading?
                            dwnloading = false;
                    }
                    else
                    {
                        // Look through the servers list and start download form idle ones
                        bool dlbusy = false;
                        for (server in ServerList)
                            if (!server.busy)
                                if (!failedFrags.empty())   // At least one fragment has to be redownloaded
                                {
                                    Download(dlfile, failedFrags.fetch(), server);
                                    server.busy = true;
                                }
                                else if (lastFrag <= maxFrag)
                                {
                                    Download(dlfile, lastFrag, server);
                                    server.busy = true;
                                    lastFrag++;
                                }
                     }
                }
                if (monitor.ulReady())
                {
                    // Upload file on every server;
                    ulFile = monitor.ulfetch()
                    for (server in ServerList)
                        Upload(ulFile, server);
                }
                if (monitor.cmdReady())
                    cmd = monitor.cmdfetch();
                    if (cmd = FILELIST)     // Get the list from one server (since they all share same files)
                        Command(monitor.cmdfetch(), ServerList[0]);
                    else
                        for (server in ServerList)      // rename, delete etc - pass to all servers
                            Command(monitor.cmdfetch(), ServerList[server]);
            */
            }
        }
    }
}
