﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAPS.Commands
{
    class CommandExit:Command
    {
        public override NetworkFrame toNetworkFrame() { return null; }
    }
}