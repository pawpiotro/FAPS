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
        private BlockingCollection<Command> Queue = new BlockingCollection<Command>(10000); //TODO ograniczenie
        private BlockingCollection<CommandChunk> uploadChunkQueue = new BlockingCollection<CommandChunk>(10000);
        private CommandChunk[] downloadBuffer;
        private int bufferSize;

        public BlockingCollection<CommandChunk> UploadChunkQueue
        {
            get { return uploadChunkQueue; }
            set { uploadChunkQueue = value; }
        }

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

        public Command Fetch()
        {
            try
            {
                return Queue.Take(token);
            }
            catch (OperationCanceledException oce)
            {
                //Console.WriteLine(oce.ToString());
                return null;
            }
        }
        
        public int Count() { return Queue.Count; }
        
    }
}
