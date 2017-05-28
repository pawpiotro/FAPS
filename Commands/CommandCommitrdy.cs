namespace FAPS.Commands
{
    class CommandCommitRdy : Command
    {
        public CommandCommitRdy() { }
        public CommandCommitRdy(NetworkFrame nf) : base(nf) { }

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.COMMITRDY);
        }
    }
}