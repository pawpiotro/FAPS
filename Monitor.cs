using System;
using System.Collections.Generic;

namespace FAPS
{
    class Monitor
    {
        private object syncObject = new object();

        private Queue<Command> uploadQueue = new Queue<Command>();
        private Queue<Command> downloadQueue = new Queue<Command>();
        private Queue<Command> miscQueue = new Queue<Command>();

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
                miscQueue.Enqueue(cmd);
            }
        }

        public void queueUpload(Command cmd)
        {
            lock (syncObject)
            { 
                uploadQueue.Enqueue(cmd);
            }
        }

        public void queueDownload(Command cmd)
        {
            lock (syncObject)
            {
                downloadQueue.Enqueue(cmd);
            }
        }

        // Is there file waiting to download/upload/command?
        public bool dlReady()
        {
            lock (syncObject)
            {
                if (downloadQueue.Count == 0)
                    return false;
                else
                    return true;
            }
        }
        public bool ulReady()
        {
            lock (syncObject)
            {
                if (uploadQueue.Count == 0)
                    return false;
                else
                    return true;
            }
        }
        public bool cmdReady()
        {
            lock (syncObject)
            {
                if (miscQueue.Count == 0)
                    return false;
                else
                    return true;
            }
        }

        // Get file/command waiting
        public Command dlFetch()
        {
            if (downloadQueue.Count == 0)
                return null;
            else
                return downloadQueue.Dequeue();
        }
        public Command ulFetch()
        {
            if (uploadQueue.Count == 0)
                return null;
            else
                return uploadQueue.Dequeue();
        }
        public Command cmdFetch()
        {
            if (miscQueue.Count == 0)
                return null;
            else
                return miscQueue.Dequeue();
        }

        public int dlCount() { return downloadQueue.Count; }
        public int ulCount() { return uploadQueue.Count; }
        public int cmdCount() { return miscQueue.Count; }

        public int dlSize()     // Return size of file to download
        {
            lock (syncObject)
            {
                return 0;
            }
        }
    }
}
