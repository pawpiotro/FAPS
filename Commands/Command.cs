namespace FAPS.Commands
{
    abstract class Command
    {
        /*
        int code;

        public int Code { get { return code; } set { code = value; } }
        */
        public Command()
        {
        }
        
        public Command(NetworkFrame nf)
        {
            //code = nf.nCode;
        }

        public abstract NetworkFrame toNetworkFrame();
    }
}
