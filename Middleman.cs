using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FAPS
{
    class Middleman
    {
        private CancellationToken token;

        private BlockingCollection<Command> uploadQueue = new BlockingCollection<Command>();
        private BlockingCollection<Command> downloadQueue = new BlockingCollection<Command>();
        private BlockingCollection<Command> miscQueue = new BlockingCollection<Command>();

        public Middleman(CancellationToken _token)
        {
            token = _token;
        }

        public void queueMisc(Command cmd)
        {
                miscQueue.Add(cmd, token);
        }

        public void queueUpload(Command cmd)
        {
                uploadQueue.Add(cmd, token);
        }

        public void queueDownload(Command cmd)
        {
                downloadQueue.Add(cmd, token);
        }

        // Is there file waiting to download/upload/command?
        public bool dlReady()
        {
                if (downloadQueue.Count == 0)
                    return false;
                else
                    return true;
        }
        public bool ulReady()
        {
                if (uploadQueue.Count == 0)
                    return false;
                else
                    return true;
        }
        public bool cmdReady()
        {
                if (miscQueue.Count == 0)
                    return false;
                else
                    return true;
        }

        // Get file/command waiting
        public Command dlFetch()
        {
            if (downloadQueue.Count == 0)
                return null;
            else
                return downloadQueue.Take(token);
        }
        public Command ulFetch()
        {
            if (uploadQueue.Count == 0)
                return null;
            else
                return uploadQueue.Take(token);
        }
        public Command cmdFetch()
        {
            if (miscQueue.Count == 0)
                return null;
            else
                return miscQueue.Take(token);
        }

        public int dlCount() { return downloadQueue.Count; }
        public int ulCount() { return uploadQueue.Count; }
        public int cmdCount() { return miscQueue.Count; }

        public int dlSize()     // Return size of file to download
        {
                return 0;
        }
    }
}
