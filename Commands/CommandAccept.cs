namespace FAPS.Commands
{
    class CommandAccept:Command
    {
        public CommandAccept() { }
        public CommandAccept(NetworkFrame nf) : base(nf){}

        public override NetworkFrame toNetworkFrame()
        {
            return new NetworkFrame(NetworkFrame.CMD.ACCEPT);
        }
    }
}
