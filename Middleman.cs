using System;
using System.Collections.Concurrent;
using System.Threading;
using FAPS.Commands;

namespace FAPS
{
    class Middleman
    {
        private CancellationToken token;

        /*
        private BlockingCollection<Command> uploadQueue = new BlockingCollection<Command>();
        private BlockingCollection<Command> downloadQueue = new BlockingCollection<Command>();
        private BlockingCollection<Command> miscQueue = new BlockingCollection<Command>();
        */
        private BlockingCollection<Command> Queue = new BlockingCollection<Command>();
        private BlockingCollection<CommandChunk> uploadChunkQueue = new BlockingCollection<CommandChunk>();
        private CommandChunk[] downloadBuffer;
        private int bufferSize;

        public BlockingCollection<CommandChunk> UploadChunkQueue
        {
            get { return uploadChunkQueue; }
            set { uploadChunkQueue = value; }
        }

        /*public CommandChunk[] DownloadChunkQueue
        {
            get { return downloadBuffer; }
            set { downloadBuffer = value; }
        }*/

        public void setDownloadBuffer(int size)
        {
            bufferSize = size;
            downloadBuffer = new CommandChunk[size];
        }
        public void addDownloadChunk(CommandChunk chunk, int frag)
        {
            frag = frag % bufferSize;
            downloadBuffer[frag] = chunk;
        }
        public CommandChunk takeDownloadChunk(int frag)
        {
            frag = frag % bufferSize;
            return downloadBuffer[frag];
        }

        public Middleman(CancellationToken _token)
        {
            token = _token;
        }

        public void queueMisc(Command cmd)
        {
                //miscQueue.Add(cmd, token);
            Queue.Add(cmd, token);
        }

        public void queueUpload(Command cmd)
        {
                //uploadQueue.Add(cmd, token);
            Queue.Add(cmd, token);
        }

        public void queueDownload(Command cmd)
        {
                //downloadQueue.Add(cmd, token);
            Queue.Add(cmd, token);
        }

        // Is there file waiting to download/upload/Command?
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
        }*
        public bool Ready()
        {
            if (Queue.Count == 0)
                return false;
            else
                return true;
        }

        // Get file/Command waiting
        /*public Command dlFetch()
        {
            return downloadQueue.Take(token);
        }
        public Command ulFetch()
        {
            return uploadQueue.Take(token);
        }
        public Command cmdFetch()
        {
            return miscQueue.Take(token);
        }*/
        public Command Fetch()
        {
            return Queue.Take(token);
        }
        /*
        public Command dlTryFetch()
        {
            return downloadQueue.TryTake(token, 500);
        }
        public Command ulTryFetch()
        {
            return uploadQueue.TryTake(token, 500);
        }
        public Command cmdTryFetch()
        {
            return miscQueue.TryTake(token, 500);
        }*/

        /*public int dlCount() { return downloadQueue.Count; }
        public int ulCount() { return uploadQueue.Count; }
        public int cmdCount() { return miscQueue.Count; }*/
        public int Count() { return Queue.Count; }
        
    }
}
