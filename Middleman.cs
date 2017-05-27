using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FAPS
{
    class Middleman
    {
        private CancellationToken token;

        /*
        private BlockingCollection<NetworkFrame> uploadQueue = new BlockingCollection<NetworkFrame>();
        private BlockingCollection<NetworkFrame> downloadQueue = new BlockingCollection<NetworkFrame>();
        private BlockingCollection<NetworkFrame> miscQueue = new BlockingCollection<NetworkFrame>();
        */
        private BlockingCollection<NetworkFrame> Queue = new BlockingCollection<NetworkFrame>();

        public Middleman(CancellationToken _token)
        {
            token = _token;
        }

        public void queueMisc(NetworkFrame cmd)
        {
                //miscQueue.Add(cmd, token);
            Queue.Add(cmd, token);
        }

        public void queueUpload(NetworkFrame cmd)
        {
                //uploadQueue.Add(cmd, token);
            Queue.Add(cmd, token);
        }

        public void queueDownload(NetworkFrame cmd)
        {
                //downloadQueue.Add(cmd, token);
            Queue.Add(cmd, token);
        }

        // Is there file waiting to download/upload/NetworkFrame?
        /*public bool dlReady()
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
        }*/
        public bool Ready()
        {
            if (Queue.Count == 0)
                return false;
            else
                return true;
        }

        // Get file/NetworkFrame waiting
        /*public NetworkFrame dlFetch()
        {
            return downloadQueue.Take(token);
        }
        public NetworkFrame ulFetch()
        {
            return uploadQueue.Take(token);
        }
        public NetworkFrame cmdFetch()
        {
            return miscQueue.Take(token);
        }*/
        public NetworkFrame Fetch()
        {
            return Queue.Take(token);
        }
        /*
        public NetworkFrame dlTryFetch()
        {
            return downloadQueue.TryTake(token, 500);
        }
        public NetworkFrame ulTryFetch()
        {
            return uploadQueue.TryTake(token, 500);
        }
        public NetworkFrame cmdTryFetch()
        {
            return miscQueue.TryTake(token, 500);
        }*/

        /*public int dlCount() { return downloadQueue.Count; }
        public int ulCount() { return uploadQueue.Count; }
        public int cmdCount() { return miscQueue.Count; }*/
        public int Count() { return Queue.Count; }


        public int dlSize()     // Return size of file to download
        {
                return 0;
        }
    }
}
