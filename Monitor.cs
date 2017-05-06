using System;
using System.Collections.Generic;

namespace FAPS
{
    class Monitor
    {
        private object syncObject = new object();

        private List<Command> uploadQueue = new List<Command>();
        private List<Command> downloadQueue = new List<Command>();
        private List<Command> miscQueue = new List<Command>();

        int i = 0;

        public void inc()
        {
            lock (syncObject)
            {
                i++;
            }
        }

        public void print()
        {
            lock (syncObject)
            {
                i++; Console.WriteLine("monitor:" + i);
            }
        }

        public void queueMisc(Command cmd)
        {
            lock (syncObject)
            {
                miscQueue.Add(cmd);
            }
        }

        public void queueUpload(Command cmd)
        {
            lock (syncObject)
            { 
            uploadQueue.Add(cmd);
            }
        }

        public void queueDownload(Command cmd)
        {
            lock (syncObject)
            {
                downloadQueue.Add(cmd);
            }
        }

        // Is there file waiting to download/upload/command?
        public bool dlReady()
        {
            lock (syncObject)
            {
                return false;
            }
        }
        public bool ulReady()
        {
            lock (syncObject)
            {
                return false;
            }
        }
        public bool cmdReady()
        {
            lock (syncObject)
            {
                return false;
            }
        }

        // Get file/command waiting
        // public file dlFetch() { }
        // public file ulFetch() { }
        // public cmd cmdFetch() { }

        public int dlSize()     // Return size of file to download
        {
            lock (syncObject)
            {
                return 0;
            }
        }
    }
}
