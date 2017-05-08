using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FAPS
{
    class Scheduler
    {
        private Middleman monitor;
        private List <DataServerHandler> serverList; // TODO change string to other type
        private bool dwnloading;
        private int lastFrag, maxFrag;
        private Queue <int> failedFrags;
        private List <bool> succFrags;
        private Command dlFile;

        public Scheduler(Middleman _monitor)
        {
            monitor = _monitor;
            monitor.print();
            dwnloading = false;
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
                dataServer = new DataServerHandler(monitor, this, t.Item1, Int32.Parse(t.Item2));
                Thread dataServerThread = new Thread(dataServer.run);
                dataServerThread.Start();
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

        private void command(Command cmd, DataServerHandler server)
        {
                server.addCmd(cmd);
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
                    succFrags = new List <bool> (maxFrag);
                    for (int i = 0; i < maxFrag; i++)
                        succFrags.Add(false);
                }
                else if (dwnloading)
                {
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
                if (monitor.ulReady())
                {
                    // Upload file on every server;
                    Command ulFile = monitor.ulFetch();
                    foreach (DataServerHandler server in serverList)
                        upload(ulFile, server);
                }
                if (monitor.cmdReady())
                {
                    Command cmd = monitor.cmdFetch();
                    /*if (cmd = FILELIST)     // Get the list from one server (since they all share same files)
                        Command(monitor.cmdfetch(), ServerList[0]);
                    else*/
                        foreach (DataServerHandler server in serverList)      // rename, delete etc - pass to all servers
                            command(cmd, server);
                }
            }
        }
    }
}
