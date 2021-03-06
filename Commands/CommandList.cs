﻿using System;

namespace FAPS.Commands
{
    class CommandList:Command
    {
        private String user;
        private CommandProcessor cmdProc;
        public CommandProcessor CmdProc { get { return cmdProc; } set { cmdProc = value; } }

        public String User { get { return user; } set { user = value; } }

        public CommandList(NetworkFrame nf) : base(nf)
        {
            user = nf.sData;
        }

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.LIST, user);
        }
    }
}
