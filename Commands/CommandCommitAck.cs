namespace FAPS.Commands
{
    class CommandCommitAck : Command
    {
        public CommandCommitAck() { }
        public CommandCommitAck(NetworkFrame nf) : base(nf) { }

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.COMMITACK);
        }
    }
}