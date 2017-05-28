namespace FAPS.Commands
{
    class CommandRollback : Command
    {
        public CommandRollback() { }
        public CommandRollback(NetworkFrame nf) : base(nf) { }

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.ROLLBACK);
        }
    }
}