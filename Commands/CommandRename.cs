using System;

namespace FAPS.Commands
{
    class CommandRename:Command
    {
        private String filename;
        private String newFilename;
        private String user;

        public String User { get { return user; } set { user = value; } }
        public String Filename { get { return filename; } set { filename = value; } }
        public String NewFilename { get { return newFilename; } set { newFilename = value; } }

        public CommandRename(NetworkFrame nf) : base(nf)
        {
            try
            {
                char[] separators = { ':' };
                String[] tmp = nf.sData.Split(separators);
                
                filename = tmp[0];
                newFilename = tmp[1];
                user = tmp[2];
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
            String data = filename + ":" + newFilename + ":" + user;
            return new NetworkFrame(NetworkFrame.CMD.RENAME, data);
        }
    }
}
