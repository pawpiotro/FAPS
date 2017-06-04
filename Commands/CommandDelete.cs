using System;

namespace FAPS.Commands
{
    class CommandDelete:Command
    {
        private String filename;
        private String user;
        private CommandProcessor cmdProc;

        public String Filename { get { return filename; } set { filename = value; } }
        public String User { get { return user; } set { user = value; } }
        public CommandProcessor CmdProc { get { return cmdProc; } set { cmdProc = value; } }

        public CommandDelete(NetworkFrame nf) : base(nf)
        {
            try
            {
                char[] separators = { ':' };
                String[] tmp = nf.sData.Split(separators);
                filename = tmp[0];
                user = tmp[1];
            }
            catch (NullReferenceException)
            {
                throw new Exception();
            }
            catch (IndexOutOfRangeException)
            {
                throw new Exception();
            }
        }

        public override NetworkFrame toNetworkFrame()
        {
            String data = filename + ":" + user;
            return new NetworkFrame(NetworkFrame.CMD.DELETE, data);
        }
    }
}
