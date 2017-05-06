using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAPS
{
    class Monitor
    {

        private List<Command> uploadQueue = new List<Command>();
        private List<Command> downloadQueue = new List<Command>();
        private List<Command> miscQueue = new List<Command>();

        int i = 0;

        public void inc() { i++; }

        public void print() { Console.WriteLine("monitor:" + i); }

        public void queueMisc(Command cmd)
        {
            miscQueue.Add(cmd);
        }

        public void queueUpload(Command cmd)
        {
            uploadQueue.Add(cmd);
        }

        public void queueDownload(Command cmd)
        {
            downloadQueue.Add(cmd);
        }

        // Is there file waiting to download/upload/command?
        public bool dlReady()
        {
            return false;
        }
        public bool ulReady()
        {
            return false;
        }
        public bool cmdReady()
        {
            return false;
        }

        // Get file/command waiting
        // public file dlFetch() { }
        // public file ulFetch() { }
        // public cmd cmdFetch() { }

        public int dlSize()     // Return size of file to download
        {
            return 0;
        }
    }
}
