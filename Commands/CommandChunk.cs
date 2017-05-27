namespace FAPS.Commands
{
    class CommandChunk:Command
    {
        private byte[] data;
        private bool sentByClient;

        public bool SentByClient
        {
            get { return sentByClient; }
            set { sentByClient = value; }
        }

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public CommandChunk(NetworkFrame nf, bool _sentByClient) : base(nf)
        {
            data = nf.Data;
            sentByClient = _sentByClient;
        }

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.CHUNK, data);
        }
    }
}
