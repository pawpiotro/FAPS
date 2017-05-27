namespace FAPS.Commands
{
    class CommandExit:Command
    {
        public CommandExit(NetworkFrame nf) : base(nf){ }

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.EXIT);
        }
    }
}
