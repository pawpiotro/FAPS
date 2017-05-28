namespace FAPS.Commands
{
    class CommandCommit : Command
    {
        public CommandCommit() { }
        public CommandCommit(NetworkFrame nf) : base(nf) { }

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.COMMIT);
        }
    }
}