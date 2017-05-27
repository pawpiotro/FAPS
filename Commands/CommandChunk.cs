namespace FAPS.Commands
{
    class CommandChunk:Command
    {
        private byte[] data;

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public CommandChunk(NetworkFrame nf) : base(nf)
        {
            data = nf.Data;
        }

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.CHUNK, data);
        }
    }
}
