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

        public void print() { Console.WriteLine(i); }
    }
}
