using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAPS
{
    class Monitor
    {
        int i = 0;

        public void inc() { i++; }

        public void print() { Console.WriteLine("monitor:" + i); }

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
